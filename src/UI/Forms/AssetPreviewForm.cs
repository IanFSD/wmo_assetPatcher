using WMO.Core.Models;
using WMO.Core.Models.Enums;
using WMO.Core.Services;
using System.Drawing;
using System.Windows.Forms;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using AssetsTools.NET.Texture;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Image = SixLabors.ImageSharp.Image;
using SystemColor = System.Drawing.Color;
using SystemRectangle = System.Drawing.Rectangle;

namespace WMO.UI.Forms
{
    public partial class AssetPreviewForm : Form
    {
        private readonly DiscoveredAsset _asset;

        public AssetPreviewForm(DiscoveredAsset asset)
        {
            InitializeComponent();
            _asset = asset;
            
            ApplyTheme();
            LoadAssetPreview();
        }

        private void ApplyTheme()
        {
        
            btnClose.FlatStyle = FlatStyle.Flat;
        }

        private void LoadAssetPreview()
        {
            lblAssetTitle.Text = $"{_asset.Name} ({_asset.AssetTypeName})";
            
            // Load properties
            LoadAssetProperties();
            
            // Try to load image preview if it's a texture
            if (_asset.AssetType == UnityAssetType.Texture2D)
            {
                LoadImagePreview();
            }
            else
            {
                // Show placeholder for non-image assets
                ShowImagePlaceholder();
            }
        }

        private void LoadAssetProperties()
        {
            try
            {
                var properties = new List<string>
                {
                    $"Asset Name: {_asset.Name}",
                    $"Type: {_asset.AssetTypeName}",
                    $"Class ID: {_asset.ClassId}",
                    $"Size: {_asset.Size:N0} bytes",
                    $"File: {Path.GetFileName(_asset.FilePath)}",
                    $"Full Path: {_asset.FilePath}",
                    $"Path ID: {_asset.PathId}",
                    "",
                    "BaseField Properties:"
                };

                // Load and scan all baseField properties like in the patching system
                var baseFieldProperties = LoadBaseFieldProperties();
                if (baseFieldProperties.Count > 0)
                {
                    properties.AddRange(baseFieldProperties);
                }
                else
                {
                    properties.Add("  (Unable to load baseField data)");
                }

                txtProperties.Text = string.Join(Environment.NewLine, properties);
            }
            catch (Exception ex)
            {
                txtProperties.Text = $"Error loading properties: {ex.Message}";
            }
        }

        private List<string> LoadBaseFieldProperties()
        {
            var properties = new List<string>();
            
            try
            {
                var manager = new AssetsManager();
                var classPackagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "lz4.tpk");
                
                if (File.Exists(classPackagePath))
                {
                    manager.LoadClassPackage(classPackagePath);
                }

                var fileInst = manager.LoadAssetsFile(_asset.FilePath, false);
                var afile = fileInst.file;
                manager.LoadClassDatabaseFromPackage(afile.Metadata.UnityVersion);
                
                var assetInfo = afile.GetAssetInfo(_asset.PathId);
                if (assetInfo != null)
                {
                    var baseField = manager.GetBaseField(fileInst, assetInfo);
                    if (baseField != null)
                    {
                        // Recursively scan all fields like in the patching system
                        ScanBaseFieldRecursive(baseField, properties, 0);
                    }
                }
                
                manager.UnloadAll(true);
            }
            catch (Exception ex)
            {
                properties.Add($"  Error reading baseField: {ex.Message}");
            }
            
            return properties;
        }

        private void ScanBaseFieldRecursive(AssetTypeValueField field, List<string> properties, int indentLevel, int maxDepth = 10)
        {
            if (field == null || indentLevel > maxDepth) return;
            
            var indent = new string(' ', indentLevel * 2);
            
            try
            {
                // Handle different field types
                switch (field.TypeName)
                {
                    case "string":
                        var stringValue = field.AsString;
                        properties.Add($"{indent}{field.FieldName}: \"{stringValue}\"");
                        break;
                        
                    case "int":
                    case "SInt32":
                        properties.Add($"{indent}{field.FieldName}: {field.AsInt}");
                        break;
                        
                    case "unsigned int":
                    case "UInt32":
                        properties.Add($"{indent}{field.FieldName}: {field.AsUInt}");
                        break;
                        
                    case "bool":
                        properties.Add($"{indent}{field.FieldName}: {field.AsBool}");
                        break;
                        
                    case "float":
                        properties.Add($"{indent}{field.FieldName}: {field.AsFloat:F3}");
                        break;
                        
                    case "double":
                        properties.Add($"{indent}{field.FieldName}: {field.AsDouble:F3}");
                        break;
                        
                    case "Array":
                        properties.Add($"{indent}{field.FieldName}: Array[{field.Children.Count}]");
                        // Show all array elements
                        for (int i = 0; i < field.Children.Count; i++)
                        {
                            var childIndent = new string(' ', (indentLevel + 1) * 2);
                            var child = field.Children[i];
                            if (child.TypeName == "string")
                            {
                                properties.Add($"{childIndent}[{i}]: \"{child.AsString}\"");
                            }
                            else if (child.TypeName == "int" || child.TypeName == "SInt32")
                            {
                                properties.Add($"{childIndent}[{i}]: {child.AsInt}");
                            }
                            else if (child.TypeName == "float")
                            {
                                properties.Add($"{childIndent}[{i}]: {child.AsFloat:F3}");
                            }
                            else
                            {
                                properties.Add($"{childIndent}[{i}]: {child.TypeName}");
                            }
                        }
                        break;
                        
                    default:
                        // For complex types, show the type name and recurse into children
                        if (field.Children?.Count > 0)
                        {
                            properties.Add($"{indent}{field.FieldName}: {field.TypeName}");
                            
                            // Recurse into all children
                            for (int i = 0; i < field.Children.Count; i++)
                            {
                                ScanBaseFieldRecursive(field.Children[i], properties, indentLevel + 1, maxDepth);
                            }
                        }
                        else
                        {
                            // Try to get a basic value
                            try
                            {
                                var value = field.AsString;
                                if (!string.IsNullOrEmpty(value))
                                {
                                    properties.Add($"{indent}{field.FieldName}: \"{value}\"");
                                }
                                else
                                {
                                    properties.Add($"{indent}{field.FieldName}: {field.TypeName} (no data)");
                                }
                            }
                            catch
                            {
                                properties.Add($"{indent}{field.FieldName}: {field.TypeName}");
                            }
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                properties.Add($"{indent}{field.FieldName}: Error reading field ({ex.Message})");
            }
        }

        private void LoadImagePreview()
        {
            try
            {
                var manager = new AssetsManager();
                var classPackagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "lz4.tpk");
                
                if (File.Exists(classPackagePath))
                {
                    manager.LoadClassPackage(classPackagePath);
                }

                var fileInst = manager.LoadAssetsFile(_asset.FilePath, false);
                var afile = fileInst.file;
                manager.LoadClassDatabaseFromPackage(afile.Metadata.UnityVersion);
                
                var inf = afile.GetAssetInfo(_asset.PathId);
                
                if (inf != null)
                {
                    var baseField = manager.GetBaseField(fileInst, inf);
                    
                    if (baseField != null && baseField["m_Name"] != null)
                    {
                        // Get texture properties
                        var width = baseField["m_Width"]?.AsInt ?? 0;
                        var height = baseField["m_Height"]?.AsInt ?? 0;
                        var format = baseField["m_TextureFormat"]?.AsInt ?? 0;
                        
                        if (width > 0 && height > 0)
                        {
                            // Try to decode the actual texture data
                            var decodedImage = DecodeTexture2D(manager, fileInst, baseField, width, height, format);
                            
                            if (decodedImage != null)
                            {
                                // Convert to System.Drawing.Bitmap for PictureBox
                                var bitmap = ConvertImageSharpToBitmap(decodedImage);
                                picPreview.Image?.Dispose(); // Clean up previous image
                                picPreview.Image = bitmap;
                                
                                // Center the image in the container if it's smaller than the container
                                CenterImageInContainer(bitmap.Width, bitmap.Height);
                                
                                decodedImage.Dispose();
                            }
                            else
                            {
                                // Fall back to texture info if decoding fails
                                ShowTextureInfo(width, height, format);
                            }
                        }
                        else
                        {
                            ShowImagePlaceholder();
                        }
                    }
                    else
                    {
                        ShowImagePlaceholder();
                    }
                }
                else
                {
                    ShowImagePlaceholder();
                }
                
                manager.UnloadAll(true);
            }
            catch (Exception ex)
            {
                ShowImagePlaceholder($"Error loading preview: {ex.Message}");
            }
        }

        private void ShowTextureInfo(int width, int height, int format)
        {
            var bitmap = new Bitmap(400, 300);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(System.Drawing.Color.Black);
                var brush = new SolidBrush(System.Drawing.Color.White);
                var font = new Font("Arial", 12, FontStyle.Bold);
                
                var lines = new[]
                {
                    "🖼️ Texture2D Preview",
                    $"Dimensions: {width} x {height}",
                    $"Format: {(TextureFormat)format} ({format})",
                    "",
                    "Pixel-perfect preview at original resolution",
                    "Format not yet supported for decoding"
                };
                
                var lineHeight = g.MeasureString("A", font).Height + 5;
                var startY = (bitmap.Height - (lines.Length * lineHeight)) / 2;
                
                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    if (string.IsNullOrEmpty(line)) continue;
                    
                    var currentFont = i == 0 ? new Font("Arial", 14, FontStyle.Bold) : font;
                    var size = g.MeasureString(line, currentFont);
                    var x = (bitmap.Width - size.Width) / 2;
                    var y = startY + (i * lineHeight);
                    
                    g.DrawString(line, currentFont, brush, x, y);
                }
                
                brush.Dispose();
            }
            
            picPreview.Image?.Dispose();
            picPreview.Image = bitmap;
            CenterImageInContainer(bitmap.Width, bitmap.Height);
        }

        private Image<Rgba32>? DecodeTexture2D(AssetsManager manager, AssetsFileInstance fileInst, AssetTypeValueField baseField, int width, int height, int format)
        {
            try
            {
                // Get texture data using AssetsTools.NET.Texture following the wiki example
                var textureFile = TextureFile.ReadTextureFile(baseField);
                var textureData = textureFile.GetTextureData(fileInst);
                
                if (textureData == null || textureData.Length == 0)
                {
                    return null;
                }

                // For now, let's try to handle simple formats like RGB24, RGBA32, etc.
                // We can expand this to support more formats later
                byte[]? rgba32Data = null;
                
                switch ((TextureFormat)format)
                {
                    case TextureFormat.RGB24:
                        rgba32Data = ConvertRGB24ToRGBA32(textureData, width * height);
                        break;
                    case TextureFormat.RGBA32:
                    case TextureFormat.ARGB32:
                        rgba32Data = textureData; // Already in correct format
                        break;
                    default:
                        // For unsupported formats, return null to fall back to texture info
                        return null;
                }
                
                if (rgba32Data == null || rgba32Data.Length == 0)
                {
                    return null;
                }

                // Create ImageSharp image from RGBA32 data
                var image = Image.LoadPixelData<Rgba32>(rgba32Data, width, height);
                
                // Flip the image vertically (Unity textures are often flipped)
                image.Mutate(x => x.Flip(FlipMode.Vertical));
                
                return image;
            }
            catch (Exception)
            {
                // Log the error but don't throw - we'll fall back to texture info
                return null;
            }
        }

        private byte[] ConvertRGB24ToRGBA32(byte[] rgb24Data, int pixelCount)
        {
            var rgba32Data = new byte[pixelCount * 4];
            
            for (int i = 0; i < pixelCount; i++)
            {
                var srcIndex = i * 3;
                var dstIndex = i * 4;
                
                rgba32Data[dstIndex] = rgb24Data[srcIndex];     // R
                rgba32Data[dstIndex + 1] = rgb24Data[srcIndex + 1]; // G
                rgba32Data[dstIndex + 2] = rgb24Data[srcIndex + 2]; // B
                rgba32Data[dstIndex + 3] = 255; // A (fully opaque)
            }
            
            return rgba32Data;
        }

        private void CenterImageInContainer(int imageWidth, int imageHeight)
        {
            // Get container size
            var containerWidth = pnlImageContainer.ClientSize.Width;
            var containerHeight = pnlImageContainer.ClientSize.Height;
            
            // Calculate position to center the image
            var x = Math.Max(0, (containerWidth - imageWidth) / 2);
            var y = Math.Max(0, (containerHeight - imageHeight) / 2);
            
            // Set the PictureBox location
            picPreview.Location = new System.Drawing.Point(x, y);
        }

        private Bitmap ConvertImageSharpToBitmap(Image<Rgba32> image)
        {
            try
            {
                var bitmap = new Bitmap(image.Width, image.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                
                // Lock bitmap data for direct pixel access
                var bitmapData = bitmap.LockBits(
                    new SystemRectangle(0, 0, bitmap.Width, bitmap.Height),
                    System.Drawing.Imaging.ImageLockMode.WriteOnly,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                try
                {
                    // Copy pixel data from ImageSharp to System.Drawing.Bitmap
                    unsafe
                    {
                        var bitmapPtr = (byte*)bitmapData.Scan0;
                        var stride = bitmapData.Stride;

                        image.ProcessPixelRows(accessor =>
                        {
                            for (int y = 0; y < accessor.Height; y++)
                            {
                                var pixelRow = accessor.GetRowSpan(y);
                                var bitmapRow = bitmapPtr + (y * stride);
                                
                                for (int x = 0; x < pixelRow.Length; x++)
                                {
                                    var pixel = pixelRow[x];
                                    var pixelIndex = x * 4;
                                    
                                    // Convert RGBA to BGRA (System.Drawing format)
                                    bitmapRow[pixelIndex] = pixel.B;     // Blue
                                    bitmapRow[pixelIndex + 1] = pixel.G; // Green
                                    bitmapRow[pixelIndex + 2] = pixel.R; // Red
                                    bitmapRow[pixelIndex + 3] = pixel.A; // Alpha
                                }
                            }
                        });
                    }
                }
                finally
                {
                    bitmap.UnlockBits(bitmapData);
                }

                return bitmap;
            }
            catch (Exception ex)
            {
                // If conversion fails, return a placeholder bitmap
                var errorBitmap = new Bitmap(300, 200);
                using (var g = Graphics.FromImage(errorBitmap))
                {
                    g.Clear(SystemColor.LightGray);
                    g.DrawString($"Error converting image:\n{ex.Message}", 
                                new Font("Arial", 9), Brushes.Red, 10, 10);
                }
                return errorBitmap;
            }
        }

        private void ShowImagePlaceholder(string? message = null)
        {
            var bitmap = new Bitmap(350, 250);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(System.Drawing.Color.Black);
                var brush = new SolidBrush(System.Drawing.Color.White);

                var font = new Font("Arial", 11);
                
                var text = message ?? $"No preview available\nfor {_asset.AssetTypeName}";
                var textSize = g.MeasureString(text, font);
                var x = (bitmap.Width - textSize.Width) / 2;
                var y = (bitmap.Height - textSize.Height) / 2;
                
                g.DrawString(text, font, brush, x, y);
                brush.Dispose();
            }
            
            picPreview.Image?.Dispose();
            picPreview.Image = bitmap;
            CenterImageInContainer(bitmap.Width, bitmap.Height);
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Clean up any resources
            if (picPreview.Image != null)
            {
                picPreview.Image.Dispose();
            }
            
            base.OnFormClosing(e);
        }
    }
}
