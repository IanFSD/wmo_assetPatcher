using WMO.Core.Logging;
using WMO.Core.Helpers;

namespace WMO.Core.Services;

/// <summary>
/// Service for handling game path validation and operations
/// </summary>
public static class GamePathService
{
    /// <summary>
    /// Validates if the given path contains a valid Whisper Mountain Outbreak installation
    /// </summary>
    /// <param name="gamePath">Path to validate</param>
    /// <returns>True if the path appears to be a valid game installation</returns>
    public static bool ValidateGamePath(string? gamePath)
    {
        try
        {
            if (string.IsNullOrEmpty(gamePath) || !Directory.Exists(gamePath))
            {
                Logger.Log(LogLevel.Debug, $"Game path validation failed: Directory does not exist");
                return false;
            }

            // Check if it's the expected game directory
            if (!gamePath.EndsWith("Whisper Mountain Outbreak", StringComparison.OrdinalIgnoreCase))
            {
                Logger.Log(LogLevel.Warning, $"Path doesn't end with 'Whisper Mountain Outbreak'");
            }

            // Look for common Unity game files
            var expectedFiles = new[] { "globalgamemanagers", "resources.assets" };
            var foundFiles = 0;
            var expectedPath = gamePath + "/Whisper Mountain Outbreak_Data";
            foreach (var expectedFile in expectedFiles)
            {
                if (File.Exists(Path.Combine(expectedPath, expectedFile)))
                {
                    foundFiles++;
                }
            }

            if (foundFiles == 0)
            {
                // Try to find any .assets files
                var assetsFiles = Directory.GetFiles(expectedPath, "*.assets", SearchOption.AllDirectories);
                if (assetsFiles.Length == 0)
                {
                    Logger.Log(LogLevel.Debug, $"No Unity assets files found in the directory");
                    return false;
                }
            }

            // Check for the game executable in the parent directory
            var gameDirectory = Directory.GetParent(expectedPath)?.FullName;
            if (!string.IsNullOrEmpty(gameDirectory))
            {
                var exePath = Path.Combine(gameDirectory, "Whisper Mountain Outbreak.exe");
                if (!File.Exists(exePath))
                {
                    Logger.Log(LogLevel.Warning, $"Game executable not found at: {exePath}");
                    return false;
                }
            }

            Logger.Log(LogLevel.Info, $"Game directory validation successful");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, $"Error validating game path: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Gets the game executable path from the data directory path
    /// </summary>
    /// <param name="gameDataPath">Path to the game data directory</param>
    /// <returns>Path to the game executable, or null if not found</returns>
    public static string? GetGameExecutablePath(string? gameDataPath)
    {
        if (string.IsNullOrEmpty(gameDataPath))
            return null;

        try
        {
            var gameDirectory = Directory.GetParent(gameDataPath)?.FullName;
            if (string.IsNullOrEmpty(gameDirectory))
                return null;

            var exePath = Path.Combine(gameDirectory, "Whisper Mountain Outbreak.exe");
            return File.Exists(exePath) ? exePath : null;
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, $"Error getting game executable path: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Launches the Whisper Mountain Outbreak game
    /// </summary>
    /// <param name="gameDataPath">Path to the game data directory</param>
    /// <returns>True if the game was launched successfully</returns>
    public static bool LaunchGame(string? gameDataPath)
    {
        try
        {
            var exePath = GetGameExecutablePath(gameDataPath);
            if (string.IsNullOrEmpty(exePath))
            {
                Logger.Log(LogLevel.Error, $"Cannot launch game: executable not found");
                return false;
            }

            Logger.Log(LogLevel.Info, $"Launching game: {exePath}");
            
            var processStartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = exePath,
                WorkingDirectory = Path.GetDirectoryName(exePath),
                UseShellExecute = true
            };

            System.Diagnostics.Process.Start(processStartInfo);
            Logger.Log(LogLevel.Info, $"Game launched successfully");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, $"Error launching game: {ex.Message}");
            return false;
        }
    }
}
