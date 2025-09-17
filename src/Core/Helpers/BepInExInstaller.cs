using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;
using WMO.Core.Logging;
using WMO.Core.Helpers;

namespace WMO.Core.Helpers;

public enum BepInExInstallError
{
    None,
    ZipNotFound,
    ExtractionFailed,
    GameExeNotFound,
    GameLaunchFailed,
    PluginsFolderNotCreated,
    UnknownError
}

public static class BepInExInstaller
{
    /// <summary>
    /// Checks if BepInEx is installed in the game directory by looking for the plugins folder
    /// </summary>
    /// <param name="gameRoot">Path to the game root directory</param>
    /// <returns>True if BepInEx plugins folder exists</returns>
    public static bool IsInstalled(string gameRoot)
    {
        try
        {
            if (string.IsNullOrEmpty(gameRoot) || !Directory.Exists(gameRoot))
                return false;
                
            string pluginsPath = Path.Combine(gameRoot, "BepInEx", "plugins");
            return Directory.Exists(pluginsPath);
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, $"Error checking BepInEx installation: {ex.Message}");
            return false;
        }
    }
    public static bool InstallAndValidate(string gameRoot, string bepinexZipPath, int waitTimeoutMs, out BepInExInstallError errorType, out string errorMessage)
    {
        errorType = BepInExInstallError.None;
        errorMessage = string.Empty;
        
        try
        {
            // 1. Check if BepInEx.zip exists
            if (!File.Exists(bepinexZipPath))
            {
                errorType = BepInExInstallError.ZipNotFound;
                errorMessage = $"BepInEx.zip not found at {bepinexZipPath}";
                Logger.Log(LogLevel.Error, $"{errorMessage}");
                return false;
            }
            
            // 2. Extract BepInEx.zip to game root
            Logger.Log(LogLevel.Info, $"Extracting BepInEx.zip to {gameRoot}");
            try
            {
                ZipFile.ExtractToDirectory(bepinexZipPath, gameRoot, true);
            }
            catch (Exception ex)
            {
                errorType = BepInExInstallError.ExtractionFailed;
                errorMessage = $"Failed to extract BepInEx.zip: {ex.Message}";
                Logger.Log(LogLevel.Error, $"{errorMessage}");
                return false;
            }

            // 3. Check if game executable exists
            string exePath = Path.Combine(gameRoot, "Whisper Mountain Outbreak.exe");
            if (!File.Exists(exePath))
            {
                errorType = BepInExInstallError.GameExeNotFound;
                errorMessage = $"Game executable not found at {exePath}";
                Logger.Log(LogLevel.Error, $"{errorMessage}");
                return false;
            }
            
            // 4. Launch game directly (not through Steam)
            Logger.Log(LogLevel.Info, $"Launching game directly to initialize BepInEx");
            Process? proc;
            try
            {
                var psi = new ProcessStartInfo(exePath)
                {
                    WorkingDirectory = gameRoot,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                proc = Process.Start(psi);
                if (proc == null)
                {
                    errorType = BepInExInstallError.GameLaunchFailed;
                    errorMessage = "Failed to start the game process";
                    Logger.Log(LogLevel.Error, $"{errorMessage}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                errorType = BepInExInstallError.GameLaunchFailed;
                errorMessage = $"Error launching game: {ex.Message}";
                Logger.Log(LogLevel.Error, $"{errorMessage}");
                return false;
            }

            // 5. Wait for plugins folder to be created
            string pluginsPath = Path.Combine(gameRoot, "BepInEx", "plugins");
            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < waitTimeoutMs)
            {
                if (Directory.Exists(pluginsPath))
                {
                    Logger.Log(LogLevel.Info, $"BepInEx plugins folder detected");
                    break;
                }
                Thread.Sleep(500);
            }
            sw.Stop();

            // 6. Kill the game process
            try { if (!proc.HasExited) proc.Kill(); } catch { }

            // 7. Verify plugins folder was created
            if (!Directory.Exists(pluginsPath))
            {
                errorType = BepInExInstallError.PluginsFolderNotCreated;
                errorMessage = $"BepInEx plugins folder was not created after {waitTimeoutMs}ms. The game may need more time to initialize BepInEx, or there might be an issue with the installation.";
                Logger.Log(LogLevel.Error, $"{errorMessage}");
                return false;
            }
            
            Logger.Log(LogLevel.Info, $"BepInEx installation completed successfully");
            return true;
        }
        catch (Exception ex)
        {
            errorType = BepInExInstallError.UnknownError;
            errorMessage = $"Unexpected error during BepInEx installation: {ex.Message}";
            Logger.Log(LogLevel.Error, $"{errorMessage}");
            ErrorHandler.Handle("BepInEx install failed", ex);
            return false;
        }
    }
    
    /// <summary>
    /// Gets a user-friendly error message for display in MessageBox
    /// </summary>
    public static string GetUserFriendlyErrorMessage(BepInExInstallError errorType, string errorMessage)
    {
        return errorType switch
        {
            BepInExInstallError.ZipNotFound => "BepInEx installation file not found. Please ensure the application was installed correctly.",
            BepInExInstallError.ExtractionFailed => $"Failed to extract BepInEx files to the game directory.\n\nDetails: {errorMessage}",
            BepInExInstallError.GameExeNotFound => "Game executable not found. Please verify the game path is correct and the game is properly installed.",
            BepInExInstallError.GameLaunchFailed => $"Failed to launch the game for BepInEx initialization.\n\nDetails: {errorMessage}",
            BepInExInstallError.PluginsFolderNotCreated => "BepInEx failed to initialize properly. The plugins folder was not created after launching the game. This may indicate an issue with the game installation or BepInEx compatibility.",
            BepInExInstallError.UnknownError => $"An unexpected error occurred during BepInEx installation.\n\nDetails: {errorMessage}",
            _ => errorMessage
        };
    }
}
