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
            // Configure logging level based on build configuration or debug flag BEFORE any logging
#if DEBUG
            SettingsService.Current.LogLevel = LogLevel.Debug; // Show debug logs in debug mode
#else
            SettingsService.Current.LogLevel = debugMode ? LogLevel.Debug : LogLevel.Info; // Show debug logs if --debug flag is used
#endif
            
            Logger.Log(LogLevel.Info, $"=== WMO Asset Patcher Started ===");

            // Clean up any outdated backup data from previous runs
            BackupManager.CleanupOutdatedBackups();

            if (consoleMode)
            {
                // Run in console mode (original behavior)
                RunConsoleMode(debugMode);
            }
            else
            {
                // Run in UI mode
                RunUIMode();
            }
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, $"Unhandled exception in main: {ex}");
            
            if (consoleMode)
            {
                Console.WriteLine();
                Console.WriteLine($" Fatal error: {ex.Message}");
#if DEBUG
                Console.WriteLine("DEBUG MODE: Exiting automatically...");
#else
                if (debugMode)
                {
                    Console.WriteLine("DEBUG MODE: Exiting automatically...");
                }
                else
                {
                    Console.WriteLine("Press any key to exit...");
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
        
        // Check if this is the first run (check if GamePath is null/empty)
        if (string.IsNullOrEmpty(SettingsService.Current.GamePath))
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
        
        Logger.Log(LogLevel.Info, $"Starting main application window");
        
        // Run the main form
        Application.Run(new MainForm());
    }

    private static void RunConsoleMode(bool debugMode)
    {
        // Original console mode logic
        Console.WriteLine("Whisper Mountain Outbreak Asset Patcher");
        Console.WriteLine("=======================================");
        Console.WriteLine();

        // Prepare mods data first
        Console.WriteLine("Preparing mod files...");
        var modsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mods");
        Console.WriteLine($"Looking for mods in: {modsPath}");
        Console.WriteLine();

        var modsCollection = ModsDataManager.GetModsCollection();
        if (modsCollection.TotalCount == 0)
        {
            Console.WriteLine(" No mod files found!");
            Console.WriteLine($"Please place your mod files in the mods folder:");
            Console.WriteLine($"{modsPath}");
            Console.WriteLine();
            Console.WriteLine("File naming: Your files should be named with the name of the asset you want to modify");
            Console.WriteLine("Example: bgm-lobby.ogg will replace 'bgm-lobby' in the game");
            Console.WriteLine();
#if DEBUG
            Console.WriteLine("DEBUG MODE: Exiting automatically...");
#else
            if (debugMode)
            {
                Console.WriteLine("DEBUG MODE: Exiting automatically...");
            }
            else
            {
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
#endif
            return;
        }

        Console.WriteLine($" Found {modsCollection.TotalCount} mod files:");
        if (modsCollection.AudioMods.Count > 0)
        {
            Console.WriteLine("   Audio mods:");
            foreach (var mod in modsCollection.AudioMods)
            {
                Console.WriteLine($"      • {mod.AssetName}");
            }
        }
        if (modsCollection.SpriteMods.Count > 0)
        {
            Console.WriteLine("   Sprite mods:");
            foreach (var mod in modsCollection.SpriteMods)
            {
                Console.WriteLine($"      • {mod.AssetName}");
            }
        }
        Console.WriteLine();

        // Get game path - different behavior for debug vs release
        string gamePath;
        
#if DEBUG
        // In debug mode, always use default path and skip user input
        Console.WriteLine("DEBUG MODE: Using default game path and skipping user input.");
        gamePath = SettingsService.DEFAULT_GAME_PATH;
        Console.WriteLine($"Using game path: {gamePath}");
        Console.WriteLine();
#else
        if (debugMode)
        {
            // Release mode with --debug flag: behave like debug mode
            Console.WriteLine("DEBUG MODE: Using default game path and skipping user input.");
            gamePath = SettingsService.DEFAULT_GAME_PATH;
            Console.WriteLine($"Using game path: {gamePath}");
            Console.WriteLine();
        }
        else
        {
            // Normal release mode: always ask user about path
            gamePath = GetGamePath();
            if (string.IsNullOrEmpty(gamePath))
            {
                Console.WriteLine(" No valid game path provided. Exiting...");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
                return;
            }

            Console.WriteLine($"Using game path: {gamePath}");
            Console.WriteLine();
        }
#endif

        // Verify the path exists and contains the game
        if (!VerifyGamePath(gamePath))
        {
            Console.WriteLine(" The specified path doesn't appear to contain Whisper Mountain Outbreak.");
#if !DEBUG
            if (!debugMode)
            {
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
#endif
            return;
        }
        // Generate asset list for user reference
        Console.WriteLine();

#if DEBUG
        // In debug mode, skip confirmation and start patching directly
        Console.WriteLine("DEBUG MODE: Starting patching process automatically...");
        Console.WriteLine();
#else      
        if (debugMode)
        {
            // Release mode with --debug flag: behave like debug mode
            Console.WriteLine("DEBUG MODE: Starting patching process automatically...");
            Console.WriteLine();
        }
        else
        {
            // Normal release mode: ask for confirmation
            Console.WriteLine("Ready to start patching. This will modify game files.");
            Console.Write("Continue? (Y/N): ");
            
            var response = Console.ReadKey().KeyChar;
            Console.WriteLine();
            Console.WriteLine();
            
            if (char.ToUpper(response) != 'Y')
            {
                Console.WriteLine("Patching cancelled.");
                if (!debugMode)
                {
                    Console.WriteLine("Press any key to exit...");
                    Console.ReadKey();
                }
                return;
            }
        }
#endif

        // Start patching process
        Console.WriteLine("Starting patching process...");
        Console.WriteLine();

        bool success = AssetPatcher.TryPatch(gamePath);

        if (success)
        {
            Console.WriteLine();
            Console.WriteLine(" Patching completed successfully!");
            Console.WriteLine("Your game has been modded. You can now run Whisper Mountain Outbreak.");
        }
        else
        {
            Console.WriteLine();
            Console.WriteLine(" Patching failed. Check the logs above for details.");
        }

        Console.WriteLine();
#if DEBUG
        Console.WriteLine("DEBUG MODE: Exiting automatically...");
#else
        if (debugMode)
        {
            Console.WriteLine("DEBUG MODE: Exiting automatically...");
        }
        else
        {
            Console.WriteLine("Press any key to exit...");
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
        Console.WriteLine("Game Path Configuration");
        Console.WriteLine("======================");
        Console.WriteLine();
        Console.WriteLine($"Default path: {SettingsService.DEFAULT_GAME_PATH}");
        Console.WriteLine();
        Console.Write("Use the default path? (Y/N): ");
        
        var useDefault = char.ToUpper(Console.ReadKey().KeyChar) == 'Y';
        Console.WriteLine();
        Console.WriteLine();

        if (useDefault)
        {
            Console.WriteLine("Using default game path.");
            return SettingsService.DEFAULT_GAME_PATH;
        }

        Console.WriteLine("Please enter the path to your game's root directory:");
        Console.WriteLine("(This should end with 'Whisper Mountain Outbreak')");
        Console.Write("Path: ");
        
        var customPath = Console.ReadLine()?.Trim();
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
                Console.WriteLine("Directory does not exist.");
                return false;
            }

            // Check if it's the expected game data directory
            if (!gamePath.EndsWith("Whisper Mountain Outbreak", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Warning: Path doesn't end with 'Whisper Mountain Outbreak'.");
                Console.WriteLine("This might not be the correct directory.");
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
                    Console.WriteLine("No Unity assets files found in the directory.");
                    return false;
                }
            }

            Console.WriteLine("Game directory appears to be valid.");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error verifying path: {ex.Message}");
            return false;
        }
    }
}
