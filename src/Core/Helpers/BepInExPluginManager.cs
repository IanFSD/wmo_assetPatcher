using WMO.Core.Logging;
using WMO.Core.Models;
using WMO.Core.Models.Enums;

namespace WMO.Core.Helpers;

/// <summary>
/// Manages dynamic BepInEx plugin installation/removal for modded game launches.
/// Copies DLLs directly into BepInEx/plugins/ and tracks which ones we installed
/// via a .wmo_managed_plugins file so we never touch user-installed plugins.
/// </summary>
public static class BepInExPluginManager
{
    private const string TrackingFileName = ".wmo_managed_plugins";

    private static string GetPluginsPath(string gameRoot)
    {
        return Path.Combine(gameRoot, "BepInEx", "plugins");
    }

    private static string GetTrackingFilePath(string gameRoot)
    {
        return Path.Combine(GetPluginsPath(gameRoot), TrackingFileName);
    }

    /// <summary>
    /// Reads the list of DLL filenames we previously installed.
    /// </summary>
    private static HashSet<string> ReadTrackedPlugins(string gameRoot)
    {
        var trackingFile = GetTrackingFilePath(gameRoot);
        if (!File.Exists(trackingFile))
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var lines = File.ReadAllLines(trackingFile)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l => l.Trim());
        return new HashSet<string>(lines, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Writes the list of DLL filenames we currently manage.
    /// </summary>
    private static void WriteTrackedPlugins(string gameRoot, IEnumerable<string> fileNames)
    {
        var trackingFile = GetTrackingFilePath(gameRoot);
        File.WriteAllLines(trackingFile, fileNames);
    }

    /// <summary>
    /// Syncs BepInEx plugin DLLs from enabled mods into BepInEx/plugins/.
    /// Copies enabled DLLs and removes any previously tracked DLLs that are no longer selected.
    /// </summary>
    /// <param name="gameRoot">Path to the game root directory</param>
    /// <param name="enabledMods">List of enabled folder mods</param>
    /// <returns>Number of plugins installed, or -1 on error</returns>
    public static int SyncPlugins(string gameRoot, IEnumerable<FolderMod> enabledMods)
    {
        try
        {
            if (!BepInExInstaller.IsInstalled(gameRoot))
            {
                Logger.Log(LogLevel.Error, $"BepInEx is not installed. Cannot sync plugins.");
                return -1;
            }

            var pluginsPath = GetPluginsPath(gameRoot);

            if (!Directory.Exists(pluginsPath))
            {
                Directory.CreateDirectory(pluginsPath);
                Logger.Log(LogLevel.Info, $"Created plugins folder: {pluginsPath}");
            }

            // Collect all BepInEx DLL files from enabled mods
            var pluginFiles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var mod in enabledMods)
            {
                foreach (var modFile in mod.ModFiles.Where(f => f.Type == ModType.BepInExPlugin && f.IsEnabled))
                {
                    var fileName = Path.GetFileName(modFile.FilePath);
                    if (!pluginFiles.ContainsKey(fileName))
                    {
                        pluginFiles[fileName] = modFile.FilePath;
                    }
                    else
                    {
                        Logger.Log(LogLevel.Warning, $"Duplicate plugin filename '{fileName}' from mod '{mod.Name}' — using first occurrence");
                    }
                }
            }

            // Remove previously tracked DLLs that are no longer in the enabled set
            var previouslyTracked = ReadTrackedPlugins(gameRoot);
            foreach (var oldName in previouslyTracked)
            {
                if (!pluginFiles.ContainsKey(oldName))
                {
                    var oldPath = Path.Combine(pluginsPath, oldName);
                    if (File.Exists(oldPath))
                    {
                        File.Delete(oldPath);
                        Logger.Log(LogLevel.Info, $"Removed unselected plugin: {oldName}");
                    }
                }
            }

            // Copy enabled DLLs to plugins folder
            int installedCount = 0;
            foreach (var (fileName, sourcePath) in pluginFiles)
            {
                try
                {
                    var destPath = Path.Combine(pluginsPath, fileName);
                    File.Copy(sourcePath, destPath, overwrite: true);
                    Logger.Log(LogLevel.Info, $"Installed plugin: {fileName}");
                    installedCount++;
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Error, $"Failed to install plugin '{fileName}': {ex.Message}");
                }
            }

            // Update the tracking file with the current set
            WriteTrackedPlugins(gameRoot, pluginFiles.Keys);

            Logger.Log(LogLevel.Success, $"Synced {installedCount} BepInEx plugin(s)");
            return installedCount;
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, $"Error syncing plugins: {ex.Message}");
            ErrorHandler.Handle("Failed to sync BepInEx plugins", ex);
            return -1;
        }
    }

    /// <summary>
    /// Removes all plugins that we previously installed (tracked in .wmo_managed_plugins).
    /// Never touches plugins the user installed manually.
    /// </summary>
    public static bool CleanManagedPlugins(string gameRoot)
    {
        try
        {
            var pluginsPath = GetPluginsPath(gameRoot);
            var tracked = ReadTrackedPlugins(gameRoot);

            if (tracked.Count == 0)
                return true;

            int removed = 0;
            foreach (var fileName in tracked)
            {
                var filePath = Path.Combine(pluginsPath, fileName);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    Logger.Log(LogLevel.Info, $"Removed managed plugin: {fileName}");
                    removed++;
                }
            }

            // Clear the tracking file
            WriteTrackedPlugins(gameRoot, Enumerable.Empty<string>());

            Logger.Log(LogLevel.Success, $"Cleaned {removed} managed plugin(s)");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, $"Error cleaning managed plugins: {ex.Message}");
            ErrorHandler.Handle("Failed to clean managed plugins", ex);
            return false;
        }
    }
}
