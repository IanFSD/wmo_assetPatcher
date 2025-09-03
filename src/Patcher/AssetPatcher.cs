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
                
                Console.WriteLine($" Error: Some game files are currently in use by another process.");
                Console.WriteLine($"Please close the following programs and try again:");
                Console.WriteLine($"  - The game itself");
                Console.WriteLine($"  - Unity Asset Bundle Extractor (UABE)");
                Console.WriteLine($"  - Any other tools that might be accessing game files");
                Console.WriteLine($"");
                Console.WriteLine($"Files in use: {string.Join(", ", lockedFiles.Select(Path.GetFileName))}");
                
                return false;
            }

            bool patchedAny = false;
            int totalPatchedAssets = 0;
            int processedFiles = 0;
            
            // Load mods data once before processing
            Logger.Log(LogLevel.Info, $"Loading mods data...");
            var modsCollection = ModsDataManager.GetModsCollection();
            Logger.Log(LogLevel.Info, $"Loaded {modsCollection.TotalAssetCount} assets from {modsCollection.ModPackages.Count} mod packages");
            Logger.Log(LogLevel.Debug, $"Audio assets: {modsCollection.GetAllAudioAssets().Count()}");
            Logger.Log(LogLevel.Debug, $"Sprite assets: {modsCollection.GetAllSpriteAssets().Count()}"); 
            Logger.Log(LogLevel.Debug, $"Texture assets: {modsCollection.GetAllTextureAssets().Count()}");
            Logger.Log(LogLevel.Debug, $"MonoBehaviour assets: {modsCollection.GetAllMonoBehaviourAssets().Count()}");
            
            if (modsCollection.TotalAssetCount == 0)
            {
                Logger.Log(LogLevel.Warning, $"No mod files found to process");
                return false;
            }

            // Process each assets file
            Console.WriteLine();
            foreach (var assetsFile in assetsFiles)
            {
                // Check if there are any mods left to patch
                if (modsCollection.TotalAssetCount == 0)
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
                    Logger.Log(LogLevel.Debug, $"Remaining assets to patch: {modsCollection.TotalAssetCount} " +
                        $"(Audio: {modsCollection.GetAllAudioAssets().Count()}, " +
                        $"Sprites: {modsCollection.GetAllSpriteAssets().Count()}, " +
                        $"Textures: {modsCollection.GetAllTextureAssets().Count()}, " +
                        $"MonoBehaviours: {modsCollection.GetAllMonoBehaviourAssets().Count()})");
                    
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
                    
                    Console.WriteLine($" Critical error while processing {fileName}: {ex.Message}");
                    Console.WriteLine($"Full error details have been logged.");
                    
                    // Stop the entire process and recover backups
                    Logger.Log(LogLevel.Error, $"Stopping patching process due to critical error in {fileName}");
                    Logger.Log(LogLevel.Info, $"Attempting to recover all files from backups...");
                    
                    if (BackupManager.RecoverBackups())
                    {
                        Logger.Log(LogLevel.Info, $"Successfully recovered all files from backups");
                        Console.WriteLine($"All files have been restored from backups due to the error.");
                    }
                    else
                    {
                        Logger.Log(LogLevel.Error, $"Failed to recover some files from backups");
                        Console.WriteLine($"Warning: Some files may not have been restored properly. Check your game installation.");
                    }
                    
                    ErrorHandler.Handle($"Critical error processing file {fileName}", ex);
                    return false;
                }
            }

            Console.WriteLine();
            Logger.Log(LogLevel.Info, $"Patching process completed. Processed {processedFiles} files total.");
            
            if (patchedAny)
            {
                Logger.Log(LogLevel.Success, $"Patching completed successfully! Total assets patched: {totalPatchedAssets}");
                Logger.Log(LogLevel.Info, $"Setting patched status to true");
                
                // Delete all backups since patching was successful
                Logger.Log(LogLevel.Info, $"Cleaning up backup files...");
                BackupManager.DeleteAllBackups();
                Logger.Log(LogLevel.Debug, $"All backup files have been deleted");
                
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
            
            Console.WriteLine($" Critical error during patching: {ex.Message}");
            Console.WriteLine($"Full error details have been logged.");
            
            // Attempt to recover from backups
            Logger.Log(LogLevel.Warning, $"Attempting to recover from backups due to error...");
            if (BackupManager.RecoverBackups())
            {
                Logger.Log(LogLevel.Info, $"Successfully recovered all files from backups");
                Console.WriteLine($"Files have been restored from backups due to the error.");
            }
            else
            {
                Logger.Log(LogLevel.Error, $"Failed to recover some files from backups");
                Console.WriteLine($"Warning: Some files may not have been restored properly. Check your game installation.");
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
            
            // Get all relevant assets from the file
            Logger.Log(LogLevel.Debug, $"Scanning for assets in {fileName}...");
            var audioAssets = afile.GetAssetsOfType((int)AssetClassID.AudioClip);
            var spriteAssets = afile.GetAssetsOfType((int)AssetClassID.Sprite);
            var textureAssets = afile.GetAssetsOfType((int)AssetClassID.Texture2D);
            var monoBehaviourAssets = afile.GetAssetsOfType((int)AssetClassID.MonoBehaviour);
            
            Logger.Log(LogLevel.Debug, $"Found assets in {fileName}: " +
                $"{audioAssets.Count} audio, {spriteAssets.Count} sprites, " +
                $"{textureAssets.Count} textures, {monoBehaviourAssets.Count} MonoBehaviours");
            
            var totalAssetsInFile = audioAssets.Count + spriteAssets.Count + textureAssets.Count + monoBehaviourAssets.Count;
            if (totalAssetsInFile == 0) 
            {
                Logger.Log(LogLevel.Debug, $"No relevant assets found in {fileName}, skipping file");
                return 0;
            }

            // Collect all replacers for this file
            var processedAssets = 0;
            var skippedAssets = 0;
            var assetsToRemove = new List<AssetBase>();

            // Process all asset types
            processedAssets += ProcessAudioAssets(manager, fileInst, audioAssets, modsCollection.GetAllAudioAssets().ToList(), assetsToRemove, fileName);
            processedAssets += ProcessSpriteAssets(manager, fileInst, spriteAssets, modsCollection.GetAllSpriteAssets().ToList(), assetsToRemove, fileName);
            processedAssets += ProcessTextureAssets(manager, fileInst, textureAssets, modsCollection.GetAllTextureAssets().ToList(), assetsToRemove, fileName);
            processedAssets += ProcessMonoBehaviourAssets(manager, fileInst, monoBehaviourAssets, modsCollection.GetAllMonoBehaviourAssets().ToList(), assetsToRemove, fileName);

            // Remove successfully patched assets from mod packages
            foreach (var assetToRemove in assetsToRemove)
            {
                RemoveAssetFromModPackages(modsCollection, assetToRemove);
                Logger.Log(LogLevel.Debug, $"Removed successfully patched asset from collection: {assetToRemove.AssetName}");
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
    /// Processes audio assets and creates replacers for matching mods
    /// </summary>
    private static int ProcessAudioAssets(AssetsManager manager, AssetsFileInstance fileInst, 
        List<AssetFileInfo> audioAssets, List<AudioAsset> audioMods, List<AssetBase> assetsToRemove, string fileName)
    {
        if (audioAssets.Count == 0 || audioMods.Count == 0) return 0;
        
        Logger.Log(LogLevel.Info, $"Processing {audioMods.Count} audio mods against {audioAssets.Count} assets in {fileName}...");
        
        var processedCount = 0;
        
        foreach (var audioMod in audioMods)
        {
            var assetName = audioMod.AssetName + audioMod.FileExtension;
            Logger.Log(LogLevel.Debug, $"Processing audio mod for asset: {assetName}");
            
            var assetData = File.ReadAllBytes(audioMod.FilePath);
            var targetAssetName = audioMod.AssetName;
            
            foreach (var assetInfo in audioAssets)
            {
                try
                {
                    var baseField = manager.GetBaseField(fileInst, assetInfo);
                    var name = baseField["m_Name"].AsString;
                    
                    if (name == targetAssetName)
                    {
                        Logger.Log(LogLevel.Debug, $"Found matching audio asset! Path ID: {assetInfo.PathId}, Name: '{name}'");
                        
                        var replacer = AudioAssetHandler.CreateReplacer(manager, fileInst, assetInfo, assetName, assetData);
                        
                        if (replacer is AssetsReplacerWrapper wrapper)
                        {
                            assetInfo.Replacer = wrapper.GetAssetsReplacer();
                            processedCount++;
                            assetsToRemove.Add(audioMod);
                            Logger.Log(LogLevel.Success, $"Successfully prepared audio replacer for: {assetName}");
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Error, $"Error processing audio asset {assetInfo.PathId}: {ex.Message}");
                    throw;
                }
            }
        }
        
        return processedCount;
    }
    
    /// <summary>
    /// Processes sprite assets and creates replacers for matching mods
    /// </summary>
    private static int ProcessSpriteAssets(AssetsManager manager, AssetsFileInstance fileInst, 
        List<AssetFileInfo> spriteAssets, List<SpriteAsset> spriteMods, List<AssetBase> assetsToRemove, string fileName)
    {
        if (spriteAssets.Count == 0 || spriteMods.Count == 0) return 0;
        
        Logger.Log(LogLevel.Info, $"Processing {spriteMods.Count} sprite mods against {spriteAssets.Count} assets in {fileName}...");
        
        var processedCount = 0;
        
        foreach (var spriteMod in spriteMods)
        {
            var assetName = spriteMod.AssetName + spriteMod.FileExtension;
            Logger.Log(LogLevel.Debug, $"Processing sprite mod for asset: {assetName}");
            
            var assetData = File.ReadAllBytes(spriteMod.FilePath);
            var targetAssetName = spriteMod.AssetName;
            
            foreach (var assetInfo in spriteAssets)
            {
                try
                {
                    var baseField = manager.GetBaseField(fileInst, assetInfo);
                    var name = baseField["m_Name"].AsString;
                    
                    if (name == targetAssetName)
                    {
                        Logger.Log(LogLevel.Debug, $"Found matching sprite asset! Path ID: {assetInfo.PathId}, Name: '{name}'");
                        
                        var replacer = SpriteAssetHandler.CreateReplacer(manager, fileInst, assetInfo, assetName, assetData);
                        
                        if (replacer is AssetsReplacerWrapper wrapper)
                        {
                            assetInfo.Replacer = wrapper.GetAssetsReplacer();
                            processedCount++;
                            assetsToRemove.Add(spriteMod);
                            Logger.Log(LogLevel.Success, $"Successfully prepared sprite replacer for: {assetName}");
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Error, $"Error processing sprite asset {assetInfo.PathId}: {ex.Message}");
                    throw;
                }
            }
        }
        
        return processedCount;
    }
    
    /// <summary>
    /// Processes texture assets and creates replacers for matching mods
    /// </summary>
    private static int ProcessTextureAssets(AssetsManager manager, AssetsFileInstance fileInst, 
        List<AssetFileInfo> textureAssets, List<TextureAsset> textureMods, List<AssetBase> assetsToRemove, string fileName)
    {
        if (textureAssets.Count == 0 || textureMods.Count == 0) return 0;
        
        Logger.Log(LogLevel.Info, $"Processing {textureMods.Count} texture mods against {textureAssets.Count} assets in {fileName}...");
        
        var processedCount = 0;
        
        foreach (var textureMod in textureMods)
        {
            var assetName = textureMod.AssetName + textureMod.FileExtension;
            Logger.Log(LogLevel.Debug, $"Processing texture mod for asset: {assetName}");
            
            var assetData = File.ReadAllBytes(textureMod.FilePath);
            var targetAssetName = textureMod.AssetName;
            
            foreach (var assetInfo in textureAssets)
            {
                try
                {
                    var baseField = manager.GetBaseField(fileInst, assetInfo);
                    var name = baseField["m_Name"].AsString;
                    
                    if (name == targetAssetName)
                    {
                        Logger.Log(LogLevel.Debug, $"Found matching texture asset! Path ID: {assetInfo.PathId}, Name: '{name}'");
                        
                        var replacer = TextureAssetHandler.CreateReplacer(manager, fileInst, assetInfo, assetName, assetData);
                        
                        if (replacer is AssetsReplacerWrapper wrapper)
                        {
                            assetInfo.Replacer = wrapper.GetAssetsReplacer();
                            processedCount++;
                            assetsToRemove.Add(textureMod);
                            Logger.Log(LogLevel.Success, $"Successfully prepared texture replacer for: {assetName}");
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Error, $"Error processing texture asset {assetInfo.PathId}: {ex.Message}");
                    throw;
                }
            }
        }
        
        return processedCount;
    }
    
    /// <summary>
    /// Processes MonoBehaviour assets and creates replacers for matching mods
    /// </summary>
    private static int ProcessMonoBehaviourAssets(AssetsManager manager, AssetsFileInstance fileInst, 
        List<AssetFileInfo> monoBehaviourAssets, List<MonoBehaviourAsset> monoBehaviourMods, List<AssetBase> assetsToRemove, string fileName)
    {
        if (monoBehaviourAssets.Count == 0 || monoBehaviourMods.Count == 0) return 0;
        
        Logger.Log(LogLevel.Info, $"Processing {monoBehaviourMods.Count} MonoBehaviour mods against {monoBehaviourAssets.Count} assets in {fileName}...");
        Logger.Log(LogLevel.Info, $"MonoBehaviour processing not yet implemented - skipping for now");
        
        // TODO: Implement MonoBehaviour processing when MonoBehaviourAssetHandler is available
        // For now, just log that we found MonoBehaviour mods but skip processing
        foreach (var monoBehaviourMod in monoBehaviourMods)
        {
            Logger.Log(LogLevel.Debug, $"Found MonoBehaviour mod (not processed): {monoBehaviourMod.AssetName}");
        }
        
        return 0;
    }
    
    /// <summary>
    /// Removes a successfully patched asset from all mod packages
    /// </summary>
    private static void RemoveAssetFromModPackages(ModsCollection modsCollection, AssetBase assetToRemove)
    {
        foreach (var modPackage in modsCollection.ModPackages)
        {
            switch (assetToRemove)
            {
                case AudioAsset audioAsset:
                    modPackage.AudioAssets.RemoveAll(a => a.AssetName == audioAsset.AssetName);
                    break;
                case SpriteAsset spriteAsset:
                    modPackage.SpriteAssets.RemoveAll(s => s.AssetName == spriteAsset.AssetName);
                    break;
                case TextureAsset textureAsset:
                    modPackage.TextureAssets.RemoveAll(t => t.AssetName == textureAsset.AssetName);
                    break;
                case MonoBehaviourAsset monoBehaviourAsset:
                    modPackage.MonoBehaviourAssets.RemoveAll(m => m.AssetName == monoBehaviourAsset.AssetName);
                    break;
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