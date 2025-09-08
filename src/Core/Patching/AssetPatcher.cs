using AssetsTools.NET;
using WMO.Core.Helpers;
using WMO.Core.Logging;
using WMO.Core.Services;
using AssetsTools.NET.Extra;
using NAudio.Wave;
using NAudio.Vorbis;

namespace WMO.Core.Patching;

public static class AssetPatcher
{
    public static bool TryPatch(string gamePath)
    {
        var modifiedFiles = new List<string>(); // Track all files that have been modified
        
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

            // Check if any assets files are locked by other processes before starting
            Logger.Log(LogLevel.Info, $"Checking if assets files are accessible...");
            var lockedFiles = CheckForLockedFiles(assetsFiles, gamePath);
            if (lockedFiles.Count > 0)
            {
                Logger.Log(LogLevel.Error, $"Cannot proceed with patching: {lockedFiles.Count} file(s) are currently in use by another process");
                Logger.Log(LogLevel.Error, $"Locked files:");
                foreach (var lockedFile in lockedFiles)
                {
                    Logger.Log(LogLevel.Error, $"  - {Path.GetFileName(lockedFile)}");
                }
                
                Logger.WriteConsole($" Error: Some game files are currently in use by another process.");
                Logger.WriteConsole($"Please close the following programs and try again:");
                Logger.WriteConsole($"  - The game itself");
                Logger.WriteConsole($"  - Unity Asset Bundle Extractor (UABE)");
                Logger.WriteConsole($"  - Any other tools that might be accessing game files");
                Logger.WriteConsole($"");
                Logger.WriteConsole($"Files in use: {string.Join(", ", lockedFiles.Select(Path.GetFileName))}");
                
                return false;
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
            Logger.WriteConsole("");
            foreach (var assetsFile in assetsFiles)
            {
                // Check if there are any mods left to patch
                if (modsCollection.TotalCount == 0)
                {
                    Logger.Log(LogLevel.Info, $"All mods have been patched successfully. Stopping processing.");
                    break;
                }

                processedFiles++;
                var fileName = Path.GetFileName(assetsFile);
                
                try
                {
                    // Create backup before processing
                    Logger.Log(LogLevel.Debug, $"Creating backup for: {fileName}");
                    var backupPath = BackupManager.CreateBackup(assetsFile);
                    if (backupPath != null)
                    {
                        Logger.Log(LogLevel.Debug, $"Backup created at: {backupPath}");
                    }
                    else
                    {
                        Logger.Log(LogLevel.Error, $"Failed to create backup for: {fileName}");
                        return false; // Abort if backup fails
                    }
                    
                    Logger.Log(LogLevel.Info, $"Processing file {processedFiles}/{assetsFiles.Length}: {fileName}");
                    Logger.Log(LogLevel.Debug, $"File path: {assetsFile}");
                    Logger.Log(LogLevel.Debug, $"File size: {new FileInfo(assetsFile).Length} bytes");
                    Logger.Log(LogLevel.Debug, $"Remaining mods to patch: {modsCollection.TotalCount} (Audio: {modsCollection.AudioMods.Count}, Sprites: {modsCollection.SpriteMods.Count}, Textures: {modsCollection.TextureMods.Count})");
                    
                    var patchedCount = PatchAssetsInFile(assetsFile, modsCollection);
                    if (patchedCount > 0)
                    {
                        totalPatchedAssets += patchedCount;
                        modifiedFiles.Add(assetsFile); // Track this file as modified
                        patchedAny = true;
                        Logger.Log(LogLevel.Success, $"Successfully patched {patchedCount} assets in {fileName}");
                    }
                    else
                    {
                        Logger.Log(LogLevel.Debug, $"No matching assets found in {fileName}");
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Error, $"Critical error while processing file {fileName}: {ex.Message}");
                    Logger.Log(LogLevel.Error, $"Exception type: {ex.GetType().FullName}");
                    Logger.Log(LogLevel.Error, $"Stack trace: {ex.StackTrace}");
                    if (ex.InnerException != null)
                    {
                        Logger.Log(LogLevel.Error, $"Inner exception: {ex.InnerException.Message}");
                        Logger.Log(LogLevel.Error, $"Inner exception type: {ex.InnerException.GetType().FullName}");
                        Logger.Log(LogLevel.Error, $"Inner exception stack trace: {ex.InnerException.StackTrace}");
                    }
                    
                    Logger.WriteConsole($" Critical error while processing {fileName}: {ex.Message}");
                    Logger.WriteConsole($"Full error details have been logged.");
                    
                    // Stop the entire process and recover backups
                    Logger.Log(LogLevel.Error, $"Stopping patching process due to critical error in {fileName}");
                    Logger.Log(LogLevel.Info, $"Attempting to recover all files from backups...");
                    
                    if (BackupManager.RecoverBackups())
                    {
                        Logger.Log(LogLevel.Info, $"Successfully recovered all files from backups");
                        Logger.WriteConsole($"All files have been restored from backups due to the error.");
                    }
                    else
                    {
                        Logger.Log(LogLevel.Error, $"Failed to recover some files from backups");
                        Logger.WriteConsole($"Warning: Some files may not have been restored properly. Check your game installation.");
                    }
                    
                    ErrorHandler.Handle($"Critical error processing file {fileName}", ex);
                    return false;
                }
            }

            Logger.WriteConsole("");
            Logger.Log(LogLevel.Info, $"Patching process completed. Processed {processedFiles} files total.");
            
            if (patchedAny)
            {
                Logger.Log(LogLevel.Success, $"Patching completed successfully! Total assets patched: {totalPatchedAssets}");
                Logger.Log(LogLevel.Info, $"Setting patched status to true");
                
                // Delete all backups since patching was successful
                Logger.Log(LogLevel.Info, $"Cleaning up backup files...");
                BackupManager.DeleteAllBackups();
                Logger.Log(LogLevel.Debug, $"All backup files have been deleted");
                
                SettingsService.Current.IsPatched = true;
                return true;
            }
            else
            {
                Logger.Log(LogLevel.Warning, $"No assets were patched. Asset names might not match mod files.");
                Logger.Log(LogLevel.Debug, $"Consider checking:");
                Logger.Log(LogLevel.Debug, $"  - File name matching between mods and game assets");
                Logger.Log(LogLevel.Debug, $"  - Asset types compatibility");
                Logger.Log(LogLevel.Debug, $"  - File path accessibility");
                Logger.WriteConsole($"No assets were patched.");
                Logger.WriteConsole($"Check if your file names match the game's asset names.");
                
                // Clean up backups since no files were actually modified
                Logger.Log(LogLevel.Debug, $"Cleaning up unused backup files...");
                BackupManager.DeleteAllBackups();
                
                return false;
            }
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, $"Critical error during patching process: {ex.Message}");
            Logger.Log(LogLevel.Error, $"Exception type: {ex.GetType().FullName}");
            Logger.Log(LogLevel.Error, $"Stack trace: {ex.StackTrace}");
            
            if (ex.InnerException != null)
            {
                Logger.Log(LogLevel.Error, $"Inner exception: {ex.InnerException.Message}");
                Logger.Log(LogLevel.Error, $"Inner exception type: {ex.InnerException.GetType().FullName}");
                Logger.Log(LogLevel.Error, $"Inner exception stack trace: {ex.InnerException.StackTrace}");
            }
            
            Logger.WriteConsole($" Critical error during patching: {ex.Message}");
            Logger.WriteConsole($"Full error details have been logged.");
            
            // Attempt to recover from backups
            Logger.Log(LogLevel.Warning, $"Attempting to recover from backups due to error...");
            if (BackupManager.RecoverBackups())
            {
                Logger.Log(LogLevel.Info, $"Successfully recovered all files from backups");
                Logger.WriteConsole($"Files have been restored from backups due to the error.");
            }
            else
            {
                Logger.Log(LogLevel.Error, $"Failed to recover some files from backups");
                Logger.WriteConsole($"Warning: Some files may not have been restored properly. Check your game installation.");
            }
            
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
            var modsToRemove = new List<AudioMod>();

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
                            Logger.Log(LogLevel.Debug, $"Found matching asset! Path ID: {assetInfo.PathId}, Name: '{name}'");
                            
                            // Create replacer for this asset - this may throw an exception
                            Logger.Log(LogLevel.Debug, $"Creating replacer for asset: {assetName}");
                            var replacer = AudioAssetHandler.CreateReplacer(manager, fileInst, assetInfo, assetName, assetData);
                            
                            if (replacer is AssetsReplacerWrapper wrapper)
                            {
                                // Set the replacer directly on the asset info
                                assetInfo.Replacer = wrapper.GetAssetsReplacer();
                                processedAssets++;
                                assetFound = true;
                                modsToRemove.Add(audioMod); // Mark for removal
                                Logger.Log(LogLevel.Success, $"Successfully prepared replacer for: {assetName} (Path ID: {assetInfo.PathId})");
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
                        Logger.Log(LogLevel.Error, $"Critical error processing asset ID {assetInfo.PathId} for mod {assetName}: {ex.Message}");
                        Logger.Log(LogLevel.Error, $"Exception type: {ex.GetType().FullName}");
                        Logger.Log(LogLevel.Error, $"Stack trace: {ex.StackTrace}");
                        if (ex.InnerException != null)
                        {
                            Logger.Log(LogLevel.Error, $"Inner exception: {ex.InnerException.Message}");
                            Logger.Log(LogLevel.Error, $"Inner exception type: {ex.InnerException.GetType().FullName}");
                            Logger.Log(LogLevel.Error, $"Inner exception stack trace: {ex.InnerException.StackTrace}");
                        }
                        
                        // Re-throw the exception to stop the patching process
                        throw new Exception($"Failed to process asset ID {assetInfo.PathId} for mod {assetName}", ex);
                    }
                }
                
                if (!assetFound)
                {
                    Logger.Log(LogLevel.Debug, $"No matching asset found for mod: {assetName}");
                    skippedAssets++;
                }
            }

            // Remove successfully patched mods from the collection
            foreach (var modToRemove in modsToRemove)
            {
                modsCollection.AudioMods.Remove(modToRemove);
                Logger.Log(LogLevel.Debug, $"Removed successfully patched mod from collection: {modToRemove.AssetName}");
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
            Logger.Log(LogLevel.Error, $"Critical error during batch processing of {fileName}: {ex.Message}");
            Logger.Log(LogLevel.Error, $"Exception type: {ex.GetType().FullName}");
            Logger.Log(LogLevel.Error, $"Stack trace: {ex.StackTrace}");
            
            if (ex.InnerException != null)
            {
                Logger.Log(LogLevel.Error, $"Inner exception: {ex.InnerException.Message}");
                Logger.Log(LogLevel.Error, $"Inner exception type: {ex.InnerException.GetType().FullName}");
                Logger.Log(LogLevel.Error, $"Inner exception stack trace: {ex.InnerException.StackTrace}");
            }
            
            // Re-throw the exception to propagate it up and stop the entire patching process
            throw new Exception($"Critical error during batch processing of {fileName}", ex);
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

    /// <summary>
    /// Checks if any of the specified files or their associated resource files are locked by other processes
    /// </summary>
    /// <param name="assetsFiles">Array of assets file paths to check</param>
    /// <param name="gamePath">Game directory path to scan for resource files</param>
    /// <returns>List of file paths that are currently locked</returns>
    private static List<string> CheckForLockedFiles(string[] assetsFiles, string gamePath)
    {
        var lockedFiles = new List<string>();
        
        Logger.Log(LogLevel.Debug, $"Checking {assetsFiles.Length} assets files for locks...");
        
        // Check all .assets files
        foreach (var assetsFile in assetsFiles)
        {
            Logger.Log(LogLevel.Trace, $"Checking if file is locked: {Path.GetFileName(assetsFile)}");
            if (IsFileLocked(assetsFile))
            {
                Logger.Log(LogLevel.Debug, $"File is locked: {assetsFile}");
                lockedFiles.Add(assetsFile);
            }
        }
        
        // Find and check all .resS files (resource files)
        Logger.Log(LogLevel.Debug, $"Scanning for resource files (.resS) in game directory...");
        var resourceFiles = Directory.GetFiles(gamePath, "*.resS", SearchOption.AllDirectories);
        Logger.Log(LogLevel.Debug, $"Found {resourceFiles.Length} resource files to check");
        
        foreach (var resourceFile in resourceFiles)
        {
            Logger.Log(LogLevel.Trace, $"Checking if resource file is locked: {Path.GetFileName(resourceFile)}");
            if (IsFileLocked(resourceFile))
            {
                Logger.Log(LogLevel.Debug, $"Resource file is locked: {resourceFile}");
                lockedFiles.Add(resourceFile);
            }
        }
        
        Logger.Log(LogLevel.Debug, $"File lock check completed. {lockedFiles.Count} files are locked");
        return lockedFiles;
    }

    /// <summary>
    /// Checks if a specific file is locked by another process
    /// </summary>
    /// <param name="filePath">Path to the file to check</param>
    /// <returns>True if the file is locked, false otherwise</returns>
    private static bool IsFileLocked(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Logger.Log(LogLevel.Trace, $"File does not exist, not locked: {filePath}");
            return false;
        }
        
        try
        {
            // Try to open the file with exclusive access
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            Logger.Log(LogLevel.Trace, $"File is not locked: {Path.GetFileName(filePath)}");
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            Logger.Log(LogLevel.Trace, $"File access denied (may be read-only or permissions issue): {filePath}");
            return true;
        }
        catch (IOException ex) when (ex.HResult == -2147024864) // 0x80070020 - The process cannot access the file because it is being used by another process
        {
            Logger.Log(LogLevel.Trace, $"File is locked by another process: {filePath}");
            return true;
        }
        catch (IOException)
        {
            Logger.Log(LogLevel.Trace, $"File has I/O issues (treating as locked): {filePath}");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Debug, $"Unexpected error checking file lock status for {filePath}: {ex.Message}");
            return true; // Treat any other exception as locked to be safe
        }
    }
}