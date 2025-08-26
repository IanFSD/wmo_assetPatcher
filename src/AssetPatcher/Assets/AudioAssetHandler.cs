using AssetsTools.NET;
using AssetsTools.NET.Extra;
using WMO.Helper;
using WMO.Logging;
using NAudio.Wave;
using NAudio.Vorbis; // Assuming this is available for OGG support; add the NuGet if needed
using FileInstance = AssetsTools.NET.Extra.AssetsFileInstance;

namespace WMO.AssetPatcher;

public class AudioAssetHandler : AssetTypeHandlerBase
{
    public AudioAssetHandler() : base(AssetClassID.AudioClip, ".wav", ".ogg", ".mp3") { }

    public bool Replace(AssetsManager am, AssetsFileInstance fileInst, string assetName, byte[] data)
    {
        try
        {
            var afile = fileInst.file;
            var audioAssets = afile.GetAssetsOfType((int)AssetClassID.AudioClip);
            string externalFilePath;
            long newOffset;
            if (audioAssets.Count == 0)
            {
                //Logger.Log(LogLevel.Warning, $"No AudioClip assets found in file: {fileInst.path}");
                return false;
            }

            string resourcePath;
            AssetTypeValueField resourceField;

            string assetsFilePath;
            string assetsDirectory;
            string temporalPath;
            foreach (var assetInfo in audioAssets)
            {
                var baseField = am.GetBaseField(fileInst, assetInfo);
                //Logger.Log(LogLevel.Info, $"Replacing audio asset: {assetName}");
                var name = baseField["m_Name"].AsString;
                resourceField = baseField["m_Resource"];
                resourcePath = resourceField["m_Source"].AsString;
                assetsFilePath = fileInst.path;
                assetsDirectory = Path.GetDirectoryName(assetsFilePath);
                if (name != assetName) continue;

                if (string.IsNullOrEmpty(resourcePath))
                {
                    externalFilePath = assetsFilePath.Replace(".temp", "") + ".resS";
                    temporalPath = externalFilePath + ".temp";
                }
                else
                {
                    externalFilePath = Path.Combine(assetsDirectory, resourcePath);
                    string resourceFileName = Path.GetFileName(resourcePath);
                    string tempFileName = resourceFileName + ".temp";
                    temporalPath = Path.Combine(assetsDirectory, tempFileName);
                    File.Copy(externalFilePath, temporalPath, true);
                    Logger.Log(LogLevel.Debug, $"Created temporary resource file: {temporalPath}");
                }

                Directory.CreateDirectory(Path.GetDirectoryName(temporalPath)!);

                using (var stream = new FileStream(
                           temporalPath,
                           FileMode.Open,
                           FileAccess.ReadWrite,
                           FileShare.None))
                {
                    newOffset = stream.Length;
                    stream.Position = newOffset;
                    stream.Write(data, 0, data.Length);
                }
                Logger.Log(LogLevel.Info, $"Replaced audio asset: {assetName} in {temporalPath}");

                // Update the asset metadata to match 
                resourceField = baseField["m_Resource"];
                resourceField["m_Offset"].AsLong = newOffset;
                resourceField["m_Size"].AsInt = data.Length;
                Logger.Log(LogLevel.Info, $"Updated asset offset to {resourceField["m_Offset"].AsLong} and size to {resourceField["m_Size"].AsInt} for: {assetName}");
                // Create a replacer for the updated asset data
                var replacer = new ContentReplacerFromBuffer(baseField.WriteToByteArray());
                assetInfo.Replacer = replacer;
                Logger.Log(LogLevel.Info, $"Updated asset metadata for: {assetName}");

                // Write changes to the file

                using (AssetsFileWriter writer = new AssetsFileWriter(temporalPath))
                {
                    afile.Write(writer);
                    Logger.Log(LogLevel.Info, $"Wrote changes to resource file: {temporalPath}");
                    }

                File.Copy(temporalPath, externalFilePath, true);
                Logger.Log(LogLevel.Info, $"Copied modified resource back to: {externalFilePath}");

                File.Delete(temporalPath);
                Logger.Log(LogLevel.Info, $"Deleted temporary resource file: {temporalPath}");
                return true;
            }
            // Use passed sample rate and channels (from original, matched via preprocessing)
            Logger.Log(LogLevel.Debug, $"audio data: {assetName} not found");
            return false;
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, $"Error replacing audio asset: {ex.Message}");
            Logger.Log(LogLevel.Debug, $"Stack trace: {ex.StackTrace}");
            ErrorHandler.Handle("Error replacing audio asset", ex);
            return false;
        }
        finally
        {
            // Any necessary cleanup can be done here
        }
    }
}