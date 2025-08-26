using System.Text.Json;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using WMO.Logging;
using WMO.AssetPatcher;

namespace WMO.Helper;

/// <summary>
/// Generates a comprehensive list of all assets in the game files
/// </summary>
public static class AssetListGenerator
{
    /// <summary>
    /// Scans all asset files in the game directory and creates a JSON file with all asset names
    /// </summary>
    /// <param name="gamePath">Path to the game's data directory</param>
    /// <param name="outputPath">Path where the JSON file should be created</param>
    /// <returns>True if the asset list was successfully generated</returns>
    public static bool GenerateAssetList(string gamePath, string outputPath)
    {
        try
        {
            Logger.Log(LogLevel.Info, $"Starting asset list generation...");
            Console.WriteLine(" Scanning game assets to create asset list...");
            Console.WriteLine("   This may take a moment...");

            var assetList = new AssetListData();
            var assetsFiles = AssetFileFinder.FindAssetsFiles(gamePath, recursive: true);

            Logger.Log(LogLevel.Info, $"Found {assetsFiles.Length} asset files to scan");
            Console.WriteLine($"   Scanning {assetsFiles.Length} asset files...");

            int totalAssets = 0;
            int processedFiles = 0;

            foreach (var assetsFile in assetsFiles)
            {
                var fileName = Path.GetFileName(assetsFile);
                Logger.Log(LogLevel.Debug, $"Scanning assets file: {fileName}");

                try
                {
                    var fileAssets = ScanSingleAssetFile(assetsFile);
                    if (fileAssets != null)
                    {
                        assetList.AssetFiles.Add(fileAssets);
                        totalAssets += fileAssets.TotalAssets;
                    }
                    processedFiles++;

                    // Show progress every few files
                    if (processedFiles % 5 == 0 || processedFiles == assetsFiles.Length)
                    {
                        Console.WriteLine($"   Progress: {processedFiles}/{assetsFiles.Length} files processed...");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Warning, $"Failed to scan {fileName}: {ex.Message}");
                }
            }

            // Sort assets alphabetically for easier browsing
            foreach (var file in assetList.AssetFiles)
            {
                file.AudioAssets.Sort();
                file.SpriteAssets.Sort();
                file.OtherAssets.Sort();
            }

            // Write JSON file
            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var jsonContent = JsonSerializer.Serialize(assetList, jsonOptions);
            File.WriteAllText(outputPath, jsonContent);

            Logger.Log(LogLevel.Info, $"Asset list generated successfully: {totalAssets} total assets in {processedFiles} files");
            Console.WriteLine($" Asset list created: {totalAssets} assets found");
            Console.WriteLine($"    Saved to: {Path.GetFileName(outputPath)}");

            return true;
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, $"Failed to generate asset list: {ex.Message}");
            Console.WriteLine($"❌ Failed to generate asset list: {ex.Message}");
            ErrorHandler.Handle("Error generating asset list", ex);
            return false;
        }
    }

    /// <summary>
    /// Scans a single asset file and extracts all asset names by type
    /// </summary>
    /// <param name="assetsFilePath">Path to the assets file</param>
    /// <returns>AssetFileData containing all assets, or null if scanning failed</returns>
    private static AssetFileData? ScanSingleAssetFile(string assetsFilePath)
    {
        AssetsManager? manager = null;
        AssetsFileInstance? fileInst = null;

        try
        {
            var fileName = Path.GetFileName(assetsFilePath);
            var assetFileData = new AssetFileData { FileName = fileName };

            manager = new AssetsManager();

            var classDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "lz4.tpk");
            manager.LoadClassPackage(classDataPath);
            fileInst = manager.LoadAssetsFile(assetsFilePath);
            manager.LoadClassDatabaseFromPackage(fileInst.file.Metadata.UnityVersion);

            // Get all assets in the file
            var allAssets = fileInst.file.GetAssetsOfType(AssetClassID.AudioClip)
                .Concat(fileInst.file.GetAssetsOfType(AssetClassID.Texture2D))
                .Concat(fileInst.file.GetAssetsOfType(AssetClassID.Sprite))
                .ToList();

            Logger.Log(LogLevel.Debug, $"Scanning {allAssets.Count} assets in {fileName}");

            foreach (var assetInfo in allAssets)
            {
                try
                {
                    var baseField = manager.GetBaseField(fileInst, assetInfo);
                    var name = baseField?["m_Name"]?.AsString;

                    if (!string.IsNullOrEmpty(name))
                    {
                        // Categorize by asset type
                        switch (assetInfo.TypeId)
                        {
                            case (int)AssetClassID.AudioClip:
                                assetFileData.AudioAssets.Add(name);
                                break;
                            case (int)AssetClassID.Texture2D:
                            case (int)AssetClassID.Sprite:
                                assetFileData.SpriteAssets.Add(name);
                                break;
                            default:
                                assetFileData.OtherAssets.Add(name);
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Debug, $"Error reading asset name: {ex.Message}");
                }
            }

            Logger.Log(LogLevel.Debug, $"Found {assetFileData.TotalAssets} assets in {fileName}: " +
                                     $"{assetFileData.AudioAssets.Count} audio, " +
                                     $"{assetFileData.SpriteAssets.Count} sprites, " +
                                     $"{assetFileData.OtherAssets.Count} other");

            return assetFileData;
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Warning, $"Failed to scan {Path.GetFileName(assetsFilePath)}: {ex.Message}");
            return null;
        }
        finally
        {
            try
            {
                fileInst?.file?.Close();
                manager?.UnloadAll();
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Debug, $"Error during cleanup: {ex.Message}");
            }
        }
    }


    /// <summary>
    /// Root data structure for the asset list JSON
    /// </summary>
    public class AssetListData
    {
        public string GeneratedAt { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        public string Version { get; set; } = "1.0";
        public string Description { get; set; } = "Complete list of all assets found in the game files";
        public List<AssetFileData> AssetFiles { get; set; } = new();

        public int TotalAudioAssets => AssetFiles.Sum(f => f.AudioAssets.Count);
        public int TotalSpriteAssets => AssetFiles.Sum(f => f.SpriteAssets.Count);
        public int TotalOtherAssets => AssetFiles.Sum(f => f.OtherAssets.Count);
        public int TotalAssets => TotalAudioAssets + TotalSpriteAssets + TotalOtherAssets;
    }

    /// <summary>
    /// Data structure representing assets in a single assets file
    /// </summary>
    public class AssetFileData
    {
        public string FileName { get; set; } = string.Empty;
        public List<string> AudioAssets { get; set; } = new();
        public List<string> SpriteAssets { get; set; } = new();
        public List<string> OtherAssets { get; set; } = new();

        public int TotalAssets => AudioAssets.Count + SpriteAssets.Count + OtherAssets.Count;
    }
}
