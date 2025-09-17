using System.Diagnostics;
using System.Security.Principal;
using WMO.Core.Patching;
using WMO.Core.Helpers;
using WMO.Core.Logging;
using WMO.Core.Services;
using WMO.UI;
using WMO.UI.Forms;

namespace WMO;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        // Enable visual styles and text rendering
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        
        // Check for console mode
        bool consoleMode = args.Contains("--console");
        bool debugMode = args.Contains("--debug");
        
        try
        {
            SettingsService.Current.ConsoleOutput = consoleMode;
            
#if DEBUG
            SettingsService.Current.LogLevel = LogLevel.Debug; // Show debug logs in debug mode
#else
            SettingsService.Current.LogLevel = debugMode ? LogLevel.Debug : LogLevel.Info; // Show debug logs if --debug flag is used
#endif
            
            Logger.Log(LogLevel.Info, $"=== WMO Asset Patcher Started ===");

            BackupManager.CleanupOutdatedBackups();

            if (consoleMode)
            {
                RunConsoleMode(debugMode);
            }
            else
            {
                RunUIMode();
            }
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Fatal, $"Unhandled exception in main: {ex}");
            
            if (consoleMode)
            {
                Logger.WriteConsole("");
                Logger.WriteConsole($" Fatal error: {ex.Message}");
#if DEBUG
                Logger.WriteConsole("DEBUG MODE: Exiting automatically...");
#else
                if (debugMode)
                {
                    Logger.WriteConsole("DEBUG MODE: Exiting automatically...");
                }
                else
                {
                    Logger.WriteConsole("Press any key to exit...");
                    Console.ReadKey();
                }
#endif
            }
            else
            {
                MessageBox.Show($"Fatal error: {ex.Message}", "Fatal Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private static void RunUIMode()
    {
        Logger.Log(LogLevel.Info, $"Starting UI mode");
        
        bool isFirstRun = string.IsNullOrEmpty(SettingsService.Current.GamePath);
        
        if (isFirstRun)
        {
            Logger.Log(LogLevel.Info, $"First run detected, showing setup form");
            using var setupForm = new SetupForm();
            var result = setupForm.ShowDialog();
            if (result != DialogResult.OK)
            {
                Logger.Log(LogLevel.Info, $"Setup cancelled by user");
                return;
            }
            Logger.Log(LogLevel.Info, $"Setup completed successfully");
        }
        
        string gameRoot = SettingsService.Current.GamePath!;
        bool bepinexCurrentlyInstalled = BepInExInstaller.IsInstalled(gameRoot);
        
        if (SettingsService.Current.BepInExInstalled != bepinexCurrentlyInstalled)
        {
            SettingsService.Current.BepInExInstalled = bepinexCurrentlyInstalled;
        }
        
        if (!bepinexCurrentlyInstalled)
        {
            Logger.Log(LogLevel.Info, $"BepInEx not detected, starting installation process");
            if (!InstallBepInEx(gameRoot))
            {
                return;
            }
            
            SettingsService.Current.BepInExInstalled = true;
        }
        else
        {
            Logger.Log(LogLevel.Info, $"BepInEx installation detected, proceeding to main application");
        }

        Logger.Log(LogLevel.Info, $"Starting main application window");
        Application.Run(new MainForm());
    }
    
    private static bool InstallBepInEx(string gameRoot)
    {
        try
        {
            string bepinexZip = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "BepInEx.zip");
            Logger.Log(LogLevel.Info, $"Beginning BepInEx installation to {gameRoot}");
            
            if (!BepInExInstaller.InstallAndValidate(gameRoot, bepinexZip, 20000, out var errorType, out var errorMessage))
            {
                string userMessage = BepInExInstaller.GetUserFriendlyErrorMessage(errorType, errorMessage);
                MessageBox.Show(userMessage, "BepInEx Installation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Logger.Log(LogLevel.Fatal, $"BepInEx install failed: {errorMessage}");
                Environment.Exit(1);
                return false;
            }
            
            MessageBox.Show("BepInEx has been installed successfully!", "BepInEx Installed", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Logger.Log(LogLevel.Info, $"BepInEx installation completed successfully");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Fatal, $"Fatal error during BepInEx setup: {ex.Message}");
            MessageBox.Show($"Fatal error during BepInEx setup: {ex.Message}", "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Environment.Exit(1);
            return false;
        }
    }

    private static void RunConsoleMode(bool debugMode)
    {
        // Original console mode logic
        Logger.WriteConsole("Whisper Mountain Outbreak Asset Patcher");
        Logger.WriteConsole("=======================================");
        Logger.WriteConsole("");

        // Prepare mods data first
        Logger.WriteConsole("Preparing mod files...");
        Logger.LogInfo("Preparing mod files for console mode");
        var modsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mods");
        Logger.WriteConsole($"Looking for mods in: {modsPath}");
        Logger.WriteConsole("");

        var modsCollection = ModsDataManager.GetModsCollection();
        if (modsCollection.TotalAssetCount == 0)
        {
            Logger.WriteConsole(" No mod files found!");
            Logger.LogWarning("No mod files found in mods directory");
            Logger.WriteConsole($"Please place your mod files in the mods folder:");
            Logger.WriteConsole($"{modsPath}");
            Logger.WriteConsole("");
            Logger.WriteConsole("File naming: Your files should be named with the name of the asset you want to modify");
            Logger.WriteConsole("Example: bgm-lobby.ogg will replace 'bgm-lobby' in the game");
            Logger.WriteConsole("");
#if DEBUG
            Logger.WriteConsole("DEBUG MODE: Exiting automatically...");
#else
            if (debugMode)
            {
                Logger.WriteConsole("DEBUG MODE: Exiting automatically...");
            }
            else
            {
                Logger.WriteConsole("Press any key to exit...");
                Console.ReadKey();
            }
#endif
            return;
        }

        Logger.WriteConsole($" Found {modsCollection.TotalAssetCount} mod files:");
        Logger.LogInfo($"Found {modsCollection.TotalAssetCount} mod files:");
        Logger.WriteConsole("");

        string gamePath;
        
#if DEBUG
        Logger.WriteConsole("DEBUG MODE: Using default game path and skipping user input.");
        Logger.LogDebug("Using default game path in debug mode");
        gamePath = SettingsService.DEFAULT_GAME_PATH;
        Logger.WriteConsole($"Using game path: {gamePath}");
        Logger.WriteConsole("");
#else
        if (debugMode)
        {
            Logger.WriteConsole("DEBUG MODE: Using default game path and skipping user input.");
            Logger.LogDebug("Using default game path in debug mode");
            gamePath = SettingsService.DEFAULT_GAME_PATH;
            Logger.WriteConsole($"Using game path: {gamePath}");
            Logger.WriteConsole("");
        }
        else
        {
            gamePath = GetGamePath();
            if (string.IsNullOrEmpty(gamePath))
            {
                Logger.WriteConsole(" No valid game path provided. Exiting...");
                Logger.LogError("No valid game path provided by user");
                Logger.WriteConsole("Press any key to exit...");
                Console.ReadKey();
                return;
            }

            Logger.WriteConsole($"Using game path: {gamePath}");
            Logger.LogInfo($"Using game path: {gamePath}");
            Logger.WriteConsole("");
        }
#endif

        if (!VerifyGamePath(gamePath))
        {
            Logger.WriteConsole(" The specified path doesn't appear to contain Whisper Mountain Outbreak.");
            Logger.LogError($"Game path verification failed for: {gamePath}");
#if !DEBUG
            if (!debugMode)
            {
                Logger.WriteConsole("Press any key to exit...");
                Console.ReadKey();
            }
#endif
            return;
        }
        Logger.WriteConsole("");

#if DEBUG
        Logger.WriteConsole("DEBUG MODE: Starting patching process automatically...");
        Logger.LogDebug("Starting patching process automatically in debug mode");
        Logger.WriteConsole("");
#else      
        if (debugMode)
        {
            Logger.WriteConsole("DEBUG MODE: Starting patching process automatically...");
            Logger.LogDebug("Starting patching process automatically in debug mode");
            Logger.WriteConsole("");
        }
        else
        {
            Logger.WriteConsole("Ready to start patching. This will modify game files.");
            Logger.WriteConsole("Continue? (Y/N): ", false);
            
            var response = Console.ReadKey().KeyChar;
            Logger.WriteConsole("");
            Logger.WriteConsole("");
            
            if (char.ToUpper(response) != 'Y')
            {
                Logger.WriteConsole("Patching cancelled.");
                Logger.LogInfo("User cancelled patching process");
                if (!debugMode)
                {
                    Logger.WriteConsole("Press any key to exit...");
                    Console.ReadKey();
                }
                return;
            }
        }
#endif

        Logger.WriteConsole("Starting patching process...");
        Logger.LogInfo("Starting patching process");
        Logger.WriteConsole("");

        bool success = AssetPatcher.TryPatch(gamePath);

        if (success)
        {
            Logger.WriteConsole("");
            Logger.WriteConsole(" Patching completed successfully!");
            Logger.WriteConsole("Your game has been modded. You can now run Whisper Mountain Outbreak.");
            Logger.LogSuccess("Patching completed successfully");
        }
        else
        {
            Logger.WriteConsole("");
            Logger.WriteConsole(" Patching failed. Check the logs above for details.");
            Logger.LogError("Patching failed");
        }

        Logger.WriteConsole("");
#if DEBUG
        Logger.WriteConsole("DEBUG MODE: Exiting automatically...");
#else
        if (debugMode)
        {
            Logger.WriteConsole("DEBUG MODE: Exiting automatically...");
        }
        else
        {
            Logger.WriteConsole("Press any key to exit...");
            Console.ReadKey();
        }
#endif
    }

    /// <summary>
    /// Gets the game path from user input, offering the default path first
    /// </summary>
    /// <returns>The selected game path, or empty string if cancelled</returns>
    private static string GetGamePath()
    {
        Logger.WriteConsole("Game Path Configuration");
        Logger.WriteConsole("======================");
        Logger.WriteConsole("");
        Logger.WriteConsole($"Default path: {SettingsService.DEFAULT_GAME_PATH}");
        Logger.WriteConsole("");
        Logger.WriteConsole("Use the default path? (Y/N): ", false);
        
        var useDefault = char.ToUpper(Console.ReadKey().KeyChar) == 'Y';
        Logger.WriteConsole("");
        Logger.WriteConsole("");

        if (useDefault)
        {
            Logger.WriteConsole("Using default game path.");
            Logger.LogInfo($"User selected default game path: {SettingsService.DEFAULT_GAME_PATH}");
            return SettingsService.DEFAULT_GAME_PATH;
        }

        Logger.WriteConsole("Please enter the path to your game's root directory:");
        Logger.WriteConsole("(This should end with 'Whisper Mountain Outbreak')");
        Logger.WriteConsole("Path: ", false);
        
        var customPath = Console.ReadLine()?.Trim();
        Logger.LogInfo($"User entered custom game path: {customPath}");
        return customPath ?? string.Empty;
    }

    /// <summary>
    /// Verifies that the given path appears to be a valid game directory
    /// </summary>
    /// <param name="gamePath">Path to verify</param>
    /// <returns>True if the path appears valid</returns>
    private static bool VerifyGamePath(string gamePath)
    {
        try
        {
            if (string.IsNullOrEmpty(gamePath) || !Directory.Exists(gamePath))
            {
                Logger.WriteConsole("Directory does not exist.");
                Logger.LogError($"Directory does not exist: {gamePath}");
                return false;
            }

            // Check if it's the expected game data directory
            if (!gamePath.EndsWith("Whisper Mountain Outbreak", StringComparison.OrdinalIgnoreCase))
            {
                Logger.WriteConsole("Warning: Path doesn't end with 'Whisper Mountain Outbreak'.");
                Logger.WriteConsole("This might not be the correct directory.");
                Logger.LogWarning($"Game path doesn't end with expected directory name: {gamePath}");
            }

            // Look for common Unity game files
            var expectedFiles = new[] { "globalgamemanagers", "resources.assets" };
            var foundFiles = 0;
            var expectedPath = Path.Combine(gamePath, "/Whisper Mountain Outbreak_Data");
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
                    Logger.WriteConsole("No Unity assets files found in the directory.");
                    Logger.LogError($"No Unity assets files found in: {expectedPath}");
                    return false;
                }
            }

            Logger.WriteConsole("Game directory appears to be valid.");
            Logger.LogInfo($"Game directory verified successfully: {gamePath}");
            return true;
        }
        catch (Exception ex)
        {
            Logger.WriteConsole($"Error verifying path: {ex.Message}");
            Logger.LogError($"Error verifying game path {gamePath}: {ex}");
            return false;
        }
    }
}
