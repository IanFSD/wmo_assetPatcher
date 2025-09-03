using System.Diagnostics;
using System.Security.Principal;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using WMO.AssetPatcher;
using WMO.Helper;
using WMO.Logging;

namespace WMO;

internal static class Program
{

    [STAThread]
    private static void Main(string[] args)
    {
        // Check for --debug command line argument
        bool debugMode = args.Contains("--debug");
        
        try
        {
            Logger.Log(LogLevel.Info, $"=== WMO Asset Patcher Started ===");
            
            // Configure logging level based on build configuration or debug flag
#if DEBUG
            SettingsHolder.LogLevel = LogLevel.Debug; // Show debug logs in debug mode
#else
            SettingsHolder.LogLevel = debugMode ? LogLevel.Debug : LogLevel.Info; // Show debug logs if --debug flag is used
#endif
            
            SettingsSaver.LoadSettings();

            // Clean up any outdated backup data from previous runs
            BackupManager.CleanupOutdatedBackups();

            // Show application info
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
            if (modsCollection.MonoBehaviourMods.Count > 0)
            {
                Console.WriteLine("   MonoBehaviour mods:");
                foreach (var mod in modsCollection.MonoBehaviourMods)
                {
                    Console.WriteLine($"      • {mod.AssetName} ({mod.FileExtension})");
                }
            }
            Console.WriteLine();

            // Get game path - different behavior for debug vs release
            string gamePath;   
#if DEBUG
            // In debug mode, always use default path and skip user input
            Console.WriteLine("DEBUG MODE: Using default game path and skipping user input.");
            gamePath = SettingsHolder.DEFAULT_GAME_PATH;
            Console.WriteLine($"Using game path: {gamePath}");
            Console.WriteLine();
#else
            if (debugMode)
            {
                // Release mode with --debug flag: behave like debug mode
                Console.WriteLine("DEBUG MODE: Using default game path and skipping user input.");
                gamePath = SettingsHolder.DEFAULT_GAME_PATH;
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

            bool success = WMO.AssetPatcher.AssetPatcher.TryPatch(gamePath);

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
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, $"Unhandled exception in main: {ex}");
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
            if (!gamePath.EndsWith("Whisper Mountain Outbreak_Data", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("⚠️  Warning: Path doesn't end with 'Whisper Mountain Outbreak_Data'.");
                Console.WriteLine("This might not be the correct directory.");
            }

            // Look for common Unity game files
            var expectedFiles = new[] { "globalgamemanagers", "resources.assets" };
            var foundFiles = 0;
            
            foreach (var expectedFile in expectedFiles)
            {
                if (File.Exists(Path.Combine(gamePath, expectedFile)))
                {
                    foundFiles++;
                }
            }

            if (foundFiles == 0)
            {
                // Try to find any .assets files
                var assetsFiles = Directory.GetFiles(gamePath, "*.assets", SearchOption.AllDirectories);
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
