using WMO.Logging;

namespace WMO.Helper;

public static class ModsDataManager
{
    private static string[]? _modsData;
    private static readonly string ResourcesPath = GetResourcesPath();
    
    public static string[] ModsData 
    { 
        get 
        { 
            _modsData ??= PrepareModsData(); 
            return _modsData; 
        } 
    }

    /// <summary>
    /// Scans the resources folder for OGG audio files, processes their names by removing the "RE" prefix,
    /// and returns an array of target asset names.
    /// </summary>
    /// <returns>Array of asset names that should be replaced</returns>
    public static string[] PrepareModsData()
    {
        try
        {
            Logger.Log(LogLevel.Info, $"Preparing mods data from resources...");

            if (!Directory.Exists(ResourcesPath))
            {
                Logger.Log(LogLevel.Warning, $"Resources directory not found at: {ResourcesPath}");
                return Array.Empty<string>();
            }

            var audioFiles = Directory.GetFiles(ResourcesPath, "*.ogg", SearchOption.AllDirectories);
            var modsData = new List<string>();

            foreach (var filePath in audioFiles)
            {
                var fileName = Path.GetFileNameWithoutExtension(filePath);
                
                // Remove "RE" prefix if it exists (case-insensitive)
                var assetName = fileName;
                if (fileName.StartsWith("RE", StringComparison.OrdinalIgnoreCase))
                {
                    assetName = fileName.Substring(2);
                    Logger.Log(LogLevel.Debug, $"Processed mod file: {fileName} -> {assetName}");
                }
                else if (fileName.StartsWith("Re", StringComparison.OrdinalIgnoreCase))
                {
                    assetName = fileName.Substring(2);
                    Logger.Log(LogLevel.Debug, $"Processed mod file: {fileName} -> {assetName}");
                }
                else
                {
                    Logger.Log(LogLevel.Warning, $"Audio file '{fileName}' doesn't start with 'RE' prefix, using as-is");
                }

                if (!string.IsNullOrEmpty(assetName))
                {
                    modsData.Add(assetName);
                }
            }

            Logger.Log(LogLevel.Info, $"Found {modsData.Count} mod files to process: {string.Join(", ", modsData)}");
            return modsData.ToArray();
        }
        catch (Exception ex)
        {
            ErrorHandler.Handle("Failed to prepare mods data", ex);
            return Array.Empty<string>();
        }
    }

    /// <summary>
    /// Gets the file path for a mod by its asset name
    /// </summary>
    /// <param name="assetName">The name of the asset to replace</param>
    /// <returns>Full path to the mod file, or null if not found</returns>
    public static string? GetModFilePath(string assetName)
    {
        try
        {
            if (!Directory.Exists(ResourcesPath))
                return null;

            // Look for files that start with "RE" + assetName
            var possibleFiles = new[]
            {
                $"RE{assetName}.ogg",
                $"Re{assetName}.ogg",
                $"{assetName}.ogg"
            };

            foreach (var possibleFile in possibleFiles)
            {
                var fullPath = Path.Combine(ResourcesPath, possibleFile);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }

            // Search in subdirectories as well
            var audioFiles = Directory.GetFiles(ResourcesPath, "*.ogg", SearchOption.AllDirectories);
            foreach (var file in audioFiles)
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                var processedName = fileName.StartsWith("RE", StringComparison.OrdinalIgnoreCase) 
                    ? fileName.Substring(2) 
                    : fileName.StartsWith("Re", StringComparison.OrdinalIgnoreCase)
                        ? fileName.Substring(2)
                        : fileName;

                if (string.Equals(processedName, assetName, StringComparison.OrdinalIgnoreCase))
                {
                    return file;
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            ErrorHandler.Handle($"Failed to get mod file path for asset '{assetName}'", ex);
            return null;
        }
    }

    /// <summary>
    /// Forces a refresh of the mods data by rescanning the resources directory
    /// </summary>
    public static void RefreshModsData()
    {
        _modsData = null;
        Logger.Log(LogLevel.Info, $"Mods data cache cleared, will be refreshed on next access");
    }

    /// <summary>
    /// Gets the path to the mods directory next to the executable
    /// </summary>
    /// <returns>Path to the mods directory</returns>
    private static string GetResourcesPath()
    {
        // Get the directory where the executable is located
        var exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var modsPath = Path.Combine(exeDirectory, "mods");
        
        // Create the mods directory if it doesn't exist
        try
        {
            if (!Directory.Exists(modsPath))
            {
                Directory.CreateDirectory(modsPath);
                Logger.Log(LogLevel.Info, $"Created mods directory at: {modsPath}");
                
                // Create a README file to explain how to use the mods folder
                var readmePath = Path.Combine(modsPath, "README.txt");
                var readmeContent = @"WMO Asset Patcher - Mods Folder
================================

Place your audio mod files (.ogg) in this folder.

File naming convention:
- Your mod files should start with 'RE' followed by the asset name
- Example: REbgm-lobby.ogg will replace the 'bgm-lobby' asset in the game

Supported audio formats:
- .ogg (recommended)
- .wav
- .mp3

Example mod files:
- REbgm-lobby.ogg
- REbgm-title.ogg
- REsfxUI-cancel.ogg
- REsfxUI-confirm.ogg
- REsfxUI-gamestart.ogg
- REsfxUI-select-1.ogg
- REsfxUI-select-2.ogg

After placing your mod files here, run the patcher to apply them to the game.
";
                File.WriteAllText(readmePath, readmeContent);
                Logger.Log(LogLevel.Info, $"Created README.txt in mods folder");
            }
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, $"Failed to create mods directory: {ex.Message}");
        }
        
        Logger.Log(LogLevel.Debug, $"Using mods directory: {modsPath}");
        return modsPath;
    }
}
