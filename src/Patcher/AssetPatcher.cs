using AssetsTools.NET;
using WMO.Helper;
using WMO.Logging;
using AssetsTools.NET.Extra;
using NAudio.Wave;
using NAudio.Vorbis;

namespace WMO.AssetPatcher;

public static class AssetPatcher
{
    public static bool TryPatch(string gamePath)
    {
        try
        {
            Logger.Log(LogLevel.Info, $"Starting patching process for game path: {gamePath}");
            Logger.Log(LogLevel.Debug, $"Verifying game directory exists...");
            
            if (!Directory.Exists(gamePath))
            {
                Logger.Log(LogLevel.Error, $"Game directory does not exist: {gamePath}");
                return false;
            }

            // Find assets files in the game directory
            Logger.Log(LogLevel.Info, $"Scanning game directory for assets files...");
            Logger.Log(LogLevel.Debug, $"Search parameters: recursive=true, path={gamePath}");
            var assetsFiles = AssetFileFinder.FindAssetsFiles(gamePath, recursive: true);
            Logger.Log(LogLevel.Info, $"Found {assetsFiles.Length} assets files to process");
            
            if (assetsFiles.Length == 0)
            {
                Logger.Log(LogLevel.Warning, $"No assets files found in game directory");
                return false;
            }

            // Log all found assets files for debugging
            Logger.Log(LogLevel.Debug, $"Assets files found:");
            foreach (var file in assetsFiles)
            {
                Logger.Log(LogLevel.Debug, $"  - {Path.GetFileName(file)}");
            }

            bool patchedAny = false;
            int totalPatchedAssets = 0;
            int processedFiles = 0;

            // Load mods data once before processing
            Logger.Log(LogLevel.Info, $"Loading mods data...");
            var modsCollection = ModsDataManager.GetModsCollection();
            Logger.Log(LogLevel.Info, $"Loaded {modsCollection.TotalCount} mod files total");
            Logger.Log(LogLevel.Debug, $"Audio mods: {modsCollection.AudioMods.Count}");
            
            if (modsCollection.TotalCount == 0)
            {
                Logger.Log(LogLevel.Warning, $"No mod files found to process");
                return false;
            }

            // Process each assets file
            Console.WriteLine();
            foreach (var assetsFile in assetsFiles)
            {
                processedFiles++;
                var fileName = Path.GetFileName(assetsFile);
                Logger.Log(LogLevel.Info, $"Processing file {processedFiles}/{assetsFiles.Length}: {fileName}");
                Logger.Log(LogLevel.Debug, $"File path: {assetsFile}");
                Logger.Log(LogLevel.Debug, $"File size: {new FileInfo(assetsFile).Length} bytes");
                
                var patchedCount = PatchAssetsInFile(assetsFile, modsCollection);
                if (patchedCount > 0)
                {
                    patchedAny = true;
                    totalPatchedAssets += patchedCount;
                    Logger.Log(LogLevel.Success, $"Successfully patched {patchedCount} assets in {fileName}");
                }
                else
                {
                    Logger.Log(LogLevel.Debug, $"No matching assets found in {fileName}");
                }
            }

            Console.WriteLine();
            Logger.Log(LogLevel.Info, $"Patching process completed. Processed {processedFiles} files total.");
            
            if (patchedAny)
            {
                Logger.Log(LogLevel.Success, $"Patching completed successfully! Total assets patched: {totalPatchedAssets}");
                Logger.Log(LogLevel.Info, $"Setting patched status to true");
                SettingsHolder.IsPatched = true;
                return true;
            }
            else
            {
                Logger.Log(LogLevel.Warning, $"No assets were patched. Asset names might not match mod files.");
                Logger.Log(LogLevel.Debug, $"Consider checking:");
                Logger.Log(LogLevel.Debug, $"  - File name matching between mods and game assets");
                Logger.Log(LogLevel.Debug, $"  - Asset types compatibility");
                Logger.Log(LogLevel.Debug, $"  - File path accessibility");
                Console.WriteLine($"No assets were patched.");
                Console.WriteLine($"Check if your file names match the game's asset names.");
                return false;
            }
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, $"Critical error during patching process: {ex.Message}");
            Logger.Log(LogLevel.Debug, $"Exception type: {ex.GetType().Name}");
            Logger.Log(LogLevel.Debug, $"Stack trace: {ex.StackTrace}");
            Console.WriteLine($" Error during patching: {ex.Message}");
            ErrorHandler.Handle("Error during patching", ex);
            return false;
        }
    }

    /// <summary>
    /// Patches audio assets in a specific assets file using mod data with batch processing
    /// Collects all replacers first, then writes all changes in a single operation
    /// </summary>
    /// <param name="assetsFilePath">Path to the assets file</param>
    /// <param name="modsCollection">Collection of mods to apply</param>
    /// <returns>Number of assets successfully patched</returns>
    private static int PatchAssetsInFile(string assetsFilePath, ModsCollection modsCollection)
    {
        AssetsManager? manager = null;
        AssetsFileInstance? fileInst = null;
        
        try
        {
            var fileName = Path.GetFileName(assetsFilePath);
            Logger.Log(LogLevel.Debug, $"Starting batch asset processing for: {fileName}");
            Logger.Log(LogLevel.Debug, $"Full path: {assetsFilePath}");

            // Load the assets file and class database
            Logger.Log(LogLevel.Debug, $"Initializing AssetsManager and loading class package...");
            manager = new AssetsManager();
            var classPackagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "lz4.tpk");
            Logger.Log(LogLevel.Debug, $"Loading class package from: {classPackagePath}");
            manager.LoadClassPackage(classPackagePath);
            
            Logger.Log(LogLevel.Debug, $"Loading assets file: {fileName}");
            fileInst = manager.LoadAssetsFile(assetsFilePath, false);
            
            var afile = fileInst.file;
            Logger.Log(LogLevel.Debug, $"Unity version detected: {afile.Metadata.UnityVersion}");
            Logger.Log(LogLevel.Debug, $"Loading class database for Unity version...");
            manager.LoadClassDatabaseFromPackage(afile.Metadata.UnityVersion);
            
            // Get all audio assets from the file
            Logger.Log(LogLevel.Debug, $"Scanning for AudioClip assets in {fileName}...");
            var audioAssets = afile.GetAssetsOfType((int)AssetClassID.AudioClip);
            Logger.Log(LogLevel.Debug, $"Found {audioAssets.Count} AudioClip assets in {fileName}");
            
            if (audioAssets.Count == 0) 
            {
                Logger.Log(LogLevel.Debug, $"No audio assets found in {fileName}, skipping file");
                return 0;
            }

            // Log audio asset names for debugging
            Logger.Log(LogLevel.Debug, $"Audio assets in {fileName}:");
            foreach (var assetInfo in audioAssets)
            {
                try
                {
                    var baseField = manager.GetBaseField(fileInst, assetInfo);
                    var assetName = baseField["m_Name"].AsString;
                    Logger.Log(LogLevel.Trace, $"  - Asset ID {assetInfo.PathId}: '{assetName}'");
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Debug, $"  - Asset ID {assetInfo.PathId}: (failed to read name - {ex.Message})");
                }
            }

            // Collect all replacers for this file
            var processedAssets = 0;
            var skippedAssets = 0;

            Logger.Log(LogLevel.Info, $"Processing {modsCollection.AudioMods.Count} audio mods against {audioAssets.Count} assets...");

            foreach (var audioMod in modsCollection.AudioMods)
            {
                var assetName = audioMod.AssetName + audioMod.FileExtension;
                Logger.Log(LogLevel.Debug, $"Processing mod for asset: {assetName}");
                
                // Find the corresponding mod file
                Logger.Log(LogLevel.Trace, $"Looking for mod file: {Path.GetFileNameWithoutExtension(assetName)}");
                var modFilePath = ModsDataManager.GetModFilePath(Path.GetFileNameWithoutExtension(assetName));
                
                if (string.IsNullOrEmpty(modFilePath))
                {
                    Logger.Log(LogLevel.Debug, $"Mod file path not found for: {assetName}");
                    skippedAssets++;
                    continue;
                }
                
                if (!File.Exists(modFilePath))
                {
                    Logger.Log(LogLevel.Warning, $"Mod file does not exist: {modFilePath}");
                    skippedAssets++;
                    continue;
                }
                
                Logger.Log(LogLevel.Debug, $"Loading mod file data from: {modFilePath}");
                var fileInfo = new FileInfo(modFilePath);
                Logger.Log(LogLevel.Debug, $"Mod file size: {fileInfo.Length} bytes");
                var assetData = File.ReadAllBytes(modFilePath);
                
                // Find the matching asset in the assets file
                bool assetFound = false;
                var targetAssetName = Path.GetFileNameWithoutExtension(assetName);
                Logger.Log(LogLevel.Debug, $"Searching for matching asset with name: '{targetAssetName}'");
                
                foreach (var assetInfo in audioAssets)
                {
                    try
                    {
                        var baseField = manager.GetBaseField(fileInst, assetInfo);
                        var name = baseField["m_Name"].AsString;
                        
                        Logger.Log(LogLevel.Trace, $"Comparing asset '{name}' with target '{targetAssetName}'");
                        
                        if (name == targetAssetName)
                        {
                            Logger.Log(LogLevel.Debug, $"Found matching asset! Asset ID: {assetInfo.PathId}, Name: '{name}'");
                            
                            // Create replacer for this asset
                            Logger.Log(LogLevel.Debug, $"Creating replacer for asset: {assetName}");
                            var replacer = AudioAssetHandler.CreateReplacer(manager, fileInst, assetInfo, assetName, assetData);
                            
                            if (replacer is AssetsReplacerWrapper wrapper)
                            {
                                // Set the replacer directly on the asset info
                                assetInfo.Replacer = wrapper.GetAssetsReplacer();
                                processedAssets++;
                                assetFound = true;
                                Logger.Log(LogLevel.Success, $"Successfully prepared replacer for: {assetName} (Asset ID: {assetInfo.PathId})");
                                break; // Found the asset, move to next mod
                            }
                            else
                            {
                                Logger.Log(LogLevel.Warning, $"Failed to create replacer for: {assetName}");
                                skippedAssets++;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(LogLevel.Debug, $"Error processing asset ID {assetInfo.PathId}: {ex.Message}");
                        continue;
                    }
                }
                
                if (!assetFound)
                {
                    Logger.Log(LogLevel.Debug, $"No matching asset found for mod: {assetName}");
                    skippedAssets++;
                }
            }

            Logger.Log(LogLevel.Info, $"Asset processing summary for {fileName}: {processedAssets} processed, {skippedAssets} skipped");

            if (processedAssets == 0)
            {
                Logger.Log(LogLevel.Debug, $"No matching assets found in {fileName} - no changes to write");
                return 0;
            }

            // Write all changes in batch
            Logger.Log(LogLevel.Info, $"Writing {processedAssets} replacers to {fileName}");
            Logger.Log(LogLevel.Debug, $"Creating temporary file for safe writing...");
            
            // Create temp file for writing
            var tempPath = assetsFilePath + ".temp";
            Logger.Log(LogLevel.Debug, $"Temp file path: {tempPath}");
            
            Logger.Log(LogLevel.Debug, $"Copying original file to temp location...");
            File.Copy(assetsFilePath, tempPath, true);
            
            Logger.Log(LogLevel.Debug, $"Writing modified assets to temp file...");
            using (var writer = new AssetsFileWriter(tempPath))
            {
                afile.Write(writer);
            }
            
            // Atomically replace the original file
            Logger.Log(LogLevel.Debug, $"Unloading assets manager...");
            manager.UnloadAll(false);
            
            Logger.Log(LogLevel.Debug, $"Atomically replacing original file with modified version...");
            File.Replace(tempPath, assetsFilePath, null);
            
            Logger.Log(LogLevel.Success, $"Successfully wrote {processedAssets} assets to {fileName}");
            return processedAssets;
        }
        catch (Exception ex)
        {
            var fileName = Path.GetFileName(assetsFilePath);
            Logger.Log(LogLevel.Error, $"Error during batch processing of {fileName}: {ex.Message}");
            Logger.Log(LogLevel.Debug, $"Exception type: {ex.GetType().Name}");
            Logger.Log(LogLevel.Debug, $"Full stack trace: {ex.StackTrace}");
            
            if (ex.InnerException != null)
            {
                Logger.Log(LogLevel.Debug, $"Inner exception: {ex.InnerException.Message}");
            }
            
            ErrorHandler.Handle("Error during batch processing", ex);
            return 0;
        }
        finally
        {
            // Clean up resources
            Logger.Log(LogLevel.Debug, $"Cleaning up resources for {Path.GetFileName(assetsFilePath)}...");
            try
            {
                manager?.UnloadAll(true);
                Logger.Log(LogLevel.Debug, $"Successfully cleaned up AssetsManager resources");
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Warning, $"Error during resource cleanup: {ex.Message}");
            }
        }
    }
}