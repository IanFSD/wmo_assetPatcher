using AssetsTools.NET;
using WMO.Helper;
using WMO.Logging;
using AssetsTools.NET.Extra;

namespace WMO.AssetPatcher;

public static class AssetPatcher
{

    public static bool TryPatch(string gamePath)
    {
        try
        {
            Logger.Log(LogLevel.Info, $"Starting patching process...");
            Console.WriteLine(" Initializing patcher...");
            //TODO: DO Work on BackupManager

            // Prepare mods data from resources
            Logger.Log(LogLevel.Info, $"Loading data...");
            Console.WriteLine(" Loading data...");
            var modsCollection = ModsDataManager.GetModsCollection();
            if (modsCollection.TotalCount == 0)
            {
                Logger.Log(LogLevel.Warning, $"No files found to process");
                Console.WriteLine("No files found to process");
                return false;
            }

            Logger.Log(LogLevel.Info, $"Found {modsCollection.TotalCount} files to apply - {modsCollection.AudioMods.Count} audio, {modsCollection.SpriteMods.Count} sprites");
            Console.WriteLine($"Found {modsCollection.TotalCount} files to apply");

            // Find assets files in the game directory
            Logger.Log(LogLevel.Info, $"Scanning game directory for assets files...");
            Console.WriteLine("Scanning game directory for assets files...");
            var assetsFiles = AssetFileFinder.FindAssetsFiles(gamePath, recursive: true);
            Logger.Log(LogLevel.Info, $"Found {assetsFiles.Length} assets files to process");
            Console.WriteLine($"Found {assetsFiles.Length} assets files to process");

            bool patchedAny = false;
            int totalPatchedAssets = 0;

            // Process each assets file
            Console.WriteLine();
            Console.WriteLine("Processing assets files:");
            foreach (var assetsFile in assetsFiles)
            {
                var fileName = Path.GetFileName(assetsFile);
                Logger.Log(LogLevel.Info, $"Processing assets file: {fileName}");
                Console.WriteLine($"Processing: {fileName}");
                
                var patchedCount = PatchAudioAssetsInFile(assetsFile, modsCollection);
                if (patchedCount > 0)
                {
                    patchedAny = true;
                    totalPatchedAssets += patchedCount;
                    Console.WriteLine($"       Patched {patchedCount} assets in {fileName}");
                }
                else
                {
                    Console.WriteLine($"       No matching assets found in {fileName}");
                }
            }

            Console.WriteLine();
            if (patchedAny)
            {
                Logger.Log(LogLevel.Info, $"Patching completed successfully! Total assets patched: {totalPatchedAssets}");
                Console.WriteLine($" Patching completed successfully!");
                Console.WriteLine($" Total assets patched: {totalPatchedAssets}");
                SettingsHolder.IsPatched = true;
                return true;
            }
            else
            {
                Logger.Log(LogLevel.Warning, $"No assets were patched. Asset names might not match mod files.");
                Console.WriteLine($"No assets were patched.");
                Console.WriteLine($"Check if your file names match the game's asset names.");
                return false;
            }
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, $"Error during patching: {ex.Message}");
            Console.WriteLine($" Error during patching: {ex.Message}");
            ErrorHandler.Handle("Error during patching", ex);
            return false;
        }
    }
    
    public static bool PatchAudioAssets(string assetsFilePath, string modAudioFolder)
    {
        try
        {
            var manager = new AssetsManager();
            var fileInst = manager.LoadAssetsFile(assetsFilePath);
            manager.LoadClassPackage("Resources/lz4.tpk");
            var afile = fileInst.file;
            manager.LoadClassDatabaseFromPackage(afile.Metadata.UnityVersion);
            var audioHandler = new AudioAssetHandler();

            foreach (var file in Directory.GetFiles(modAudioFolder))
            {
                var extension = Path.GetExtension(file).ToLower();
                if (!audioHandler.Extensions.Contains(extension))
                    continue;

                var assetName = Path.GetFileNameWithoutExtension(file);
                var assetData = File.ReadAllBytes(file);

                // Find the asset in the assets file
                var assetInfo = fileInst.file.GetAssetsOfType(audioHandler.ClassId)
                    .FirstOrDefault(a =>
                        manager.GetBaseField(fileInst, a)["m_Name"].AsString == assetName);

                if (assetInfo == null)
                {
                    Logger.Log(LogLevel.Error, $"Audio asset '{assetName}' not found in assets file.");
                    continue;
                }

                var replaced = audioHandler.Replace(manager, fileInst, assetInfo, assetData);
                if (replaced)
                    Logger.Log(LogLevel.Info, $"Patched audio asset: {assetName}");
                else
                    Logger.Log(LogLevel.Error, $"Failed to patch audio asset: {assetName}");
            }

            // Save changes
            var outputFile = assetsFilePath + ".new";
            using (var writer = new AssetsFileWriter(outputFile))
                fileInst.file.Write(writer, -1);

            fileInst.file.Close();
            BackupManager.CreateBackup(assetsFilePath);
            File.Copy(outputFile, assetsFilePath, true);
            File.Delete(outputFile);

            Logger.Log(LogLevel.Info, $"Audio assets patching complete.");
            return true;
        }
        catch (Exception ex)
        {
            ErrorHandler.Handle("Error patching audio assets", ex);
            return false;
        }
    }

    /// <summary>
    /// Patches audio assets in a specific assets file using mod data
    /// Processes ONE asset at a time and saves immediately to prevent data corruption
    /// </summary>
    /// <param name="assetsFilePath">Path to the assets file</param>
    /// <param name="modsCollection">Collection of mods to apply</param>
    /// <returns>Number of assets successfully patched</returns>
    private static int PatchAudioAssetsInFile(string assetsFilePath, ModsCollection modsCollection)
    {
        int totalPatchedCount = 0;
        var fileName = Path.GetFileName(assetsFilePath);
        
        Logger.Log(LogLevel.Debug, $"Starting individual asset processing for: {fileName}");
        
        // Process each audio mod individually to prevent corruption
        foreach (var audioMod in modsCollection.AudioMods)
        {
            var patchResult = ProcessSingleAssetInFile(assetsFilePath, audioMod.AssetName);
            if (patchResult.Success)
            {
                totalPatchedCount++;
                Console.WriteLine($"         →  Patched '{audioMod.AssetName}' in {fileName}");
                Logger.Log(LogLevel.Info, $"✓ Successfully patched and saved '{audioMod.AssetName}' in {fileName}");
            }
            else if (patchResult.AssetFound)
            {
                Console.WriteLine($"         →  Failed to patch '{audioMod.AssetName}' in {fileName}");
                Logger.Log(LogLevel.Error, $"✗ Failed to patch '{audioMod.AssetName}' in {fileName}: {patchResult.ErrorMessage}");
            }
            // If asset not found, we continue silently (it might be in another assets file)
        }
        
        // TODO: Add support for sprite mods later
        // For now, we only process audio mods
        
        return totalPatchedCount;
    }
    
    /// <summary>
    /// Processes a single asset in an assets file - finds, patches, and saves immediately
    /// </summary>
    /// <param name="assetsFilePath">Path to the assets file</param>
    /// <param name="assetName">Name of the asset to patch</param>
    /// <returns>Result of the patching operation</returns>
    private static PatchResult ProcessSingleAssetInFile(string assetsFilePath, string assetName)
    {
        AssetsManager? manager = null;
        AssetsFileInstance? fileInst = null;
        
        try
        {
            Logger.Log(LogLevel.Debug, $"Processing single asset '{assetName}' in {Path.GetFileName(assetsFilePath)}");
            
            // Find the corresponding mod file
            var modFilePath = ModsDataManager.GetModFilePath(assetName);
            if (string.IsNullOrEmpty(modFilePath) || !File.Exists(modFilePath))
            {
                return new PatchResult { Success = false, AssetFound = false, ErrorMessage = "Mod file not found" };
            }

            // Load the assets file fresh for this single operation
            manager = new AssetsManager();
            fileInst = manager.LoadAssetsFile(assetsFilePath);
            
            // Try to load class database (optional)
            bool classDbLoaded = TryLoadClassDatabase(manager, fileInst.file);
            if (!classDbLoaded)
            {
                Logger.Log(LogLevel.Debug, $"Proceeding without class database for {assetName}");
            }

            // Get all audio assets from the file
            var audioAssets = fileInst.file.GetAssetsOfType(AssetClassID.AudioClip);
            Logger.Log(LogLevel.Debug, $"Scanning {audioAssets.Count} audio assets for '{assetName}'");

            // Find the target asset
            AssetFileInfo? targetAsset = null;
            foreach (var assetInfo in audioAssets)
            {
                try
                {
                    var baseField = manager.GetBaseField(fileInst, assetInfo);
                    var name = baseField?["m_Name"]?.AsString;
                    
                    if (string.Equals(name, assetName, StringComparison.OrdinalIgnoreCase))
                    {
                        targetAsset = assetInfo;
                        Logger.Log(LogLevel.Debug, $"Found target asset '{assetName}' with ID {assetInfo.PathId}");
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Debug, $"Error reading asset name: {ex.Message}");
                }
            }

            if (targetAsset == null)
            {
                // Asset not found in this file - this is normal, it might be in another assets file
                Logger.Log(LogLevel.Debug, $"Asset '{assetName}' not found in {Path.GetFileName(assetsFilePath)}");
                return new PatchResult { Success = false, AssetFound = false, ErrorMessage = "Asset not found in this file" };
            }

            // Load the mod file data
            var modData = File.ReadAllBytes(modFilePath);
            Logger.Log(LogLevel.Debug, $"Loaded {modData.Length} bytes from {Path.GetFileName(modFilePath)}");

            // Replace the asset
            var audioHandler = new AudioAssetHandler();
            var replaced = audioHandler.Replace(manager, fileInst, targetAsset, modData);
            
            if (!replaced)
            {
                return new PatchResult { Success = false, AssetFound = true, ErrorMessage = "Asset replacement failed" };
            }

            // IMMEDIATELY save the changes to prevent corruption
            Logger.Log(LogLevel.Debug, $"Immediately saving changes for '{assetName}' to prevent corruption");
            
            var outputFile = assetsFilePath + ".patch_temp";
            using (var writer = new AssetsFileWriter(outputFile))
            {
                fileInst.file.Write(writer, -1);
            }

            // Clean up resources before file operations
            fileInst.file.Close();
            fileInst = null;
            manager = null;

            // Create backup and replace original atomically
            BackupManager.CreateBackup(assetsFilePath);
            File.Move(outputFile, assetsFilePath, overwrite: true);
            
            Logger.Log(LogLevel.Info, $"✓ Successfully patched and saved '{assetName}' in {Path.GetFileName(assetsFilePath)}");
            return new PatchResult { Success = true, AssetFound = true, ErrorMessage = null };
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, $"Error processing asset '{assetName}': {ex.Message}");
            return new PatchResult { Success = false, AssetFound = true, ErrorMessage = ex.Message };
        }
        finally
        {
            // Ensure resources are always cleaned up
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
    /// Attempts to load class database from various possible locations
    /// </summary>
    /// <param name="manager">AssetsManager instance</param>
    /// <param name="assetsFile">Assets file to get Unity version from</param>
    /// <returns>True if class database was successfully loaded</returns>
    private static bool TryLoadClassDatabase(AssetsManager manager, AssetsFile assetsFile)
    {
        // Try using built-in class database from Unity version first (recommended approach)
        try
        {
            var unityVersion = assetsFile.Metadata.UnityVersion;
            Logger.Log(LogLevel.Debug, $"Trying to load class database for Unity version: {unityVersion}");
            
            // Try to load from TPK package if available
            string[] possibleTpkPaths = {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "*.tpk"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "*.tpk"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lz4.tpk"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "lz4.tpk"),
                Path.Combine(Environment.CurrentDirectory, "*.tpk"),
                Path.Combine(Environment.CurrentDirectory, "lz4.tpk")
            };

            foreach (var tpkPath in possibleTpkPaths)
            {
                try
                {
                    if (File.Exists(tpkPath))
                    {
                        manager.LoadClassPackage(tpkPath);
                        manager.LoadClassDatabaseFromPackage(unityVersion);
                        Logger.Log(LogLevel.Debug, $"Successfully loaded class database from TPK: {tpkPath}");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Debug, $"Failed to load TPK from {tpkPath}: {ex.Message}");
                }
            }

            // Try legacy .dat files
            string[] possibleDatPaths = {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cldb.dat"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "cldb.dat"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"cldb_{unityVersion}.dat"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", $"cldb_{unityVersion}.dat"),
                Path.Combine(Environment.CurrentDirectory, "cldb.dat"),
                Path.Combine(Environment.CurrentDirectory, $"cldb_{unityVersion}.dat")
            };

            foreach (var datPath in possibleDatPaths)
            {
                try
                {
                    if (File.Exists(datPath))
                    {
                        manager.LoadClassDatabase(datPath);
                        Logger.Log(LogLevel.Debug, $"Successfully loaded class database from DAT: {datPath}");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Debug, $"Failed to load DAT from {datPath}: {ex.Message}");
                }
            }

            Logger.Log(LogLevel.Debug, $"No external class database found, using built-in defaults");
            return false;
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Debug, $"Error in TryLoadClassDatabase: {ex.Message}");
            return false;
        }
    }
}

/// <summary>
/// Result of a single asset patching operation
/// </summary>
internal class PatchResult
{
    public bool Success { get; set; }
    public bool AssetFound { get; set; }
    public string? ErrorMessage { get; set; }
}
