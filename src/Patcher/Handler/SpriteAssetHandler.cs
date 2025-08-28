using AssetsTools.NET;
using AssetsTools.NET.Extra;
using AssetsTools.NET.Texture;
using WMO.Helper;
using WMO.Logging;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace WMO.AssetPatcher;

public class SpriteAssetHandler : AssetTypeHandlerBase
{
    public SpriteAssetHandler() : base(AssetClassID.Sprite, ".png", ".jpg", ".jpeg", ".bmp", ".tga") { }

    public static IAssetReplacer? CreateReplacer(AssetsManager am,
                                                 AssetsFileInstance fileInst,
                                                 AssetFileInfo assetInfo,
                                                 string assetName,
                                                 byte[] data)
    {
        try
        {
            Logger.Log(LogLevel.Debug, $"Creating replacer for sprite asset: {assetName}");
            Logger.Log(LogLevel.Debug, $"Input data size: {data.Length} bytes");

            string ext = Path.GetExtension(assetName).ToLowerInvariant();
            Logger.Log(LogLevel.Debug, $"File extension detected: {ext}");

            // Read the sprite's base field
            Logger.Log(LogLevel.Debug, $"Reading sprite base field...");
            var baseField = am.GetBaseField(fileInst, assetInfo);
            var name = baseField["m_Name"].AsString;
            Logger.Log(LogLevel.Debug, $"Sprite name from file: '{name}'");
            Logger.Log(LogLevel.Debug, $"Expected name: '{Path.GetFileNameWithoutExtension(assetName)}'");
            
            if (name != Path.GetFileNameWithoutExtension(assetName))
            {
                Logger.Log(LogLevel.Debug, $"Sprite name mismatch, skipping");
                return null;
            }

            Logger.Log(LogLevel.Debug, $"Sprite name matches, proceeding with replacement");

            // Get the texture reference from the sprite
            var textureField = baseField["m_RD"]["texture"];
            if (textureField == null)
            {
                Logger.Log(LogLevel.Warning, $"Sprite '{name}' has no texture reference");
                return null;
            }
    
            var fileId = textureField["m_FileID"].AsInt;
            var pathId = textureField["m_PathID"].AsLong;
            
            Logger.Log(LogLevel.Debug, $"Sprite references texture - FileID: {fileId}, PathID: {pathId}");

            // Find the texture asset
            AssetFileInfo? textureAssetInfo = null;
            AssetsFileInstance? textureFileInst = fileInst;

            if (fileId == 0)
            {
                // Texture is in the same file
                Logger.Log(LogLevel.Debug, $"Texture is in same file, searching for PathID: {pathId}");
                textureAssetInfo = fileInst.file.GetAssetsOfType((int)AssetClassID.Texture2D)
                    .FirstOrDefault(a => a.PathId == pathId);
            }
            else
            {
                Logger.Log(LogLevel.Warning, $"External texture references not yet supported (FileID: {fileId})");
                return null;
            }

            if (textureAssetInfo == null)
            {
                Logger.Log(LogLevel.Warning, $"Could not find texture asset for sprite '{name}'");
                return null;
            }

            Logger.Log(LogLevel.Debug, $"Found texture asset with PathID: {textureAssetInfo.PathId}");

            // Load and process the image
            Logger.Log(LogLevel.Debug, $"Loading image from mod file...");
            Bitmap? bitmap = null;
            try
            {
                using (var ms = new MemoryStream(data))
                {
                    bitmap = new Bitmap(ms);
                }
                
                Logger.Log(LogLevel.Debug, $"Image loaded successfully: {bitmap.Width}x{bitmap.Height} pixels");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, $"Failed to load image: {ex.Message}");
                return null;
            }

            // Get the texture's base field
            var textureBaseField = am.GetBaseField(textureFileInst, textureAssetInfo);
            var textureFile = TextureFile.ReadTextureFile(textureBaseField);
            
            Logger.Log(LogLevel.Debug, $"Original texture format: {(TextureFormat)textureFile.m_TextureFormat}");
            Logger.Log(LogLevel.Debug, $"Original texture dimensions: {textureFile.m_Width}x{textureFile.m_Height}");
            Logger.Log(LogLevel.Debug, $"New image dimensions: {bitmap.Width}x{bitmap.Height}");

            // Handle image resizing if needed
            if (bitmap.Width != textureFile.m_Width || bitmap.Height != textureFile.m_Height)
            {
                Logger.Log(LogLevel.Info, $"Resizing image from {bitmap.Width}x{bitmap.Height} to {textureFile.m_Width}x{textureFile.m_Height}");
                var resized = new Bitmap(textureFile.m_Width, textureFile.m_Height);
                using (var g = Graphics.FromImage(resized))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.DrawImage(bitmap, 0, 0, textureFile.m_Width, textureFile.m_Height);
                }
                bitmap.Dispose();
                bitmap = resized;
            }

            // Convert bitmap to BGRA format for Unity
            Logger.Log(LogLevel.Debug, $"Converting image to BGRA format...");
            var bgraData = BitmapToBGRA(bitmap);
            bitmap.Dispose();
            
            Logger.Log(LogLevel.Debug, $"BGRA data size: {bgraData.Length} bytes");

            // Update texture format to RGBA32 for compatibility
            Logger.Log(LogLevel.Debug, $"Setting texture format to RGBA32");
            textureFile.m_TextureFormat = (int)TextureFormat.RGBA32;
            
            // Set the new texture data
            Logger.Log(LogLevel.Debug, $"Setting new texture data...");
            textureFile.SetTextureData(bgraData, textureFile.m_Width, textureFile.m_Height);
            
            // Write the changes back to the texture field
            Logger.Log(LogLevel.Debug, $"Writing texture changes...");
            textureFile.WriteTo(textureBaseField);
            
            // Create replacer for the texture
            var textureReplacer = new ContentReplacerFromBuffer(textureBaseField.WriteToByteArray());
            textureAssetInfo.Replacer = textureReplacer;
            
            Logger.Log(LogLevel.Success, $"Successfully prepared texture replacer for sprite: {assetName}");
            
            // Note: We don't need to create a replacer for the sprite itself since we're only modifying the texture
            // The sprite will automatically use the updated texture
            Logger.Log(LogLevel.Debug, $"Sprite asset replacement completed");
            
            // Return a dummy replacer to indicate success (the actual replacement was done on the texture)
            return new AssetsReplacerWrapper(textureReplacer, textureAssetInfo.PathId);
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, $"Error preparing replacer for sprite asset '{assetName}': {ex.Message}");
            Logger.Log(LogLevel.Debug, $"Exception type: {ex.GetType().Name}");
            Logger.Log(LogLevel.Debug, $"Full stack trace: {ex.StackTrace}");
            
            if (ex.InnerException != null)
            {
                Logger.Log(LogLevel.Debug, $"Inner exception: {ex.InnerException.Message}");
            }
            
            ErrorHandler.Handle("Error preparing replacer for sprite asset", ex);
            return null;
        }
    }

    /// <summary>
    /// Converts a Bitmap to BGRA byte array format required by Unity
    /// </summary>
    /// <param name="bitmap">The bitmap to convert</param>
    /// <returns>BGRA byte array</returns>
    private static byte[] BitmapToBGRA(Bitmap bitmap)
    {
        Logger.Log(LogLevel.Debug, $"Converting bitmap to BGRA format: {bitmap.Width}x{bitmap.Height}");
        
        var bgraData = new byte[bitmap.Width * bitmap.Height * 4];
        var bitmapData = bitmap.LockBits(
            new Rectangle(0, 0, bitmap.Width, bitmap.Height),
            ImageLockMode.ReadOnly,
            PixelFormat.Format32bppArgb);

        try
        {
            Marshal.Copy(bitmapData.Scan0, bgraData, 0, bgraData.Length);

            // Convert ARGB to BGRA
            for (var i = 0; i < bgraData.Length; i += 4)
            {
                var a = bgraData[i];     // Alpha
                var r = bgraData[i + 1]; // Red
                var g = bgraData[i + 2]; // Green
                var b = bgraData[i + 3]; // Blue

                bgraData[i] = b;     // Blue
                bgraData[i + 1] = g; // Green
                bgraData[i + 2] = r; // Red
                bgraData[i + 3] = a; // Alpha
            }
            
            Logger.Log(LogLevel.Debug, $"Successfully converted {bgraData.Length} bytes to BGRA format");
        }
        finally
        {
            bitmap.UnlockBits(bitmapData);
        }

        return bgraData;
    }
}
