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
            Logger.Log(LogLevel.Info, $"Starting patching process...");

            // Prepare mods data from resources



            // Find assets files in the game directory
            Logger.Log(LogLevel.Info, $"Scanning game directory for assets files...");
            var assetsFiles = AssetFileFinder.FindAssetsFiles(gamePath, recursive: true);
            Logger.Log(LogLevel.Info, $"Found {assetsFiles.Length} assets files to process");

            bool patchedAny = false;
            int totalPatchedAssets = 0;

            // Process each assets file
            Console.WriteLine();
            foreach (var assetsFile in assetsFiles)
            {
                Logger.Log(LogLevel.Info, $"Loading data...");
                var modsCollection = ModsDataManager.GetModsCollection();
                if (modsCollection.TotalCount == 0)
                {
                    Logger.Log(LogLevel.Warning, $"No files found to process");
                    return false;
                }
                var fileName = Path.GetFileName(assetsFile);
                var patchedCount = PatchAudioAssetsInFile(assetsFile, modsCollection);
                if (patchedCount > 0)
                {
                    patchedAny = true;
                    totalPatchedAssets += patchedCount;
                    Logger.Log(LogLevel.Info, $"Patched {patchedCount} assets in {fileName}");
                }
                else
                {
                    Logger.Log(LogLevel.Info, $"No matching assets found in {fileName}");
                }
            }

            Console.WriteLine();
            if (patchedAny)
            {
                Logger.Log(LogLevel.Info, $"Patching completed successfully! Total assets patched: {totalPatchedAssets}");
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
            if (patchResult == true)
            {
                totalPatchedCount++;
                //modsCollection.AudioMods.Remove(audioMod); // Remove to avoid reprocessing
                Logger.Log(LogLevel.Info, $"Successfully patched and saved '{audioMod.AssetName}' in {fileName}");
            }
            // If asset not found, we continue silently (it might be in another assets file)
        }


        return totalPatchedCount;
    }

    /// <summary>
    /// Processes a single asset in an assets file - finds, patches, and saves immediately
    /// </summary>
    /// <param name="assetsFilePath">Path to the assets file</param>
    /// <param name="assetName">Name of the asset to patch</param>
    /// <returns>Result of the patching operation</returns>
    private static bool ProcessSingleAssetInFile(string assetsFilePath, string assetName)
    {
        AssetsManager? manager = null;
        AssetsFileInstance? fileInst = null;
        var newFile = assetsFilePath + ".temp";

        try
        {
            // Create a copy for modification
            File.Copy(assetsFilePath, newFile, true);

            // Load the class database
            manager = new AssetsManager();
            manager.LoadClassPackage(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "lz4.tpk"));

            using (var fs = new FileStream(
                newFile,
                FileMode.Open,
                FileAccess.ReadWrite,
                FileShare.None))
            {
                fileInst = manager.LoadAssetsFile(fs, newFile);
                manager.LoadClassDatabaseFromPackage(fileInst.file.Metadata.UnityVersion);

                // Find the corresponding mod file
                var modFilePath = ModsDataManager.GetModFilePath(assetName);
                if (string.IsNullOrEmpty(modFilePath) || !File.Exists(modFilePath))
                {
                    return false; // Asset not found in this file, continue silently
                }
                var assetData = File.ReadAllBytes(modFilePath);

                // Update AudioClip metadata
                var audioHandler = new AudioAssetHandler();
                if (!audioHandler.Replace(manager, fileInst, assetName, assetData))
                {
                    return false; // Failed to replace asset
                }
                

                 
            } 
            // Copy over original
            TryCopyFile(newFile, assetsFilePath, true);
            Console.WriteLine($"Successfully patched {assetsFilePath}");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, $"Error processing asset '{assetName}': {ex.Message} {ex.StackTrace}");
            return false; // Error during processing
        }
        finally
        {
            // Clean up resources
            Logger.Log(LogLevel.Debug, $"Cleaning up resources for: {fileInst?.path}");
           
            manager?.UnloadAll();
            if (File.Exists(newFile))
            {
                try
                {
                    
                    File.Delete(newFile);
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Warning, $"Failed to delete temporary file '{newFile}': {ex.Message}");
                }
            }
        }
    }

private static bool TryCopyFile(string sourcePath, string destPath, bool overwrite, int maxRetries = 3, int delayMs = 1000)
{
    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            File.Copy(sourcePath, destPath, overwrite);
            Logger.Log(LogLevel.Debug, $"File copy succeeded: {sourcePath} to {destPath}");
            return true;
        }
        catch (IOException)
        {
            if (i == maxRetries - 1) throw;
            Logger.Log(LogLevel.Info, $"Retrying file copy ({i + 1}/{maxRetries})...");
            Thread.Sleep(delayMs);
        }
    }
    return false;
}
}