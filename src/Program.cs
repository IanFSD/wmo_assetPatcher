using System.Diagnostics;
using System.Security.Principal;
using WMO.AssetPatcher;
using WMO.Helper;
using WMO.Logging;

namespace WMO;

internal static class Program
{
    private const string DEFAULT_GAME_PATH = @"C:\Program Files (x86)\Steam\steamapps\common\Whisper Mountain Outbreak\Whisper Mountain Outbreak_Data";

    [STAThread]
    private static void Main()
    {
        try
        {
            Logger.Log(LogLevel.Info, $"=== WMO Asset Patcher Started ===");
            SettingsSaver.LoadSettings();

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
                Console.WriteLine($"Please place your audio mod files (.ogg) in the mods folder:");
                Console.WriteLine($"{modsPath}");
                Console.WriteLine();
                Console.WriteLine("File naming: Your files should be named with the name of the asset you want to modify");
                Console.WriteLine("Example: bgm-lobby.ogg will replace 'bgm-lobby' in the game");
                Console.WriteLine();
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
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

            // Get game path from user
            string gamePath = GetGamePath();
            if (string.IsNullOrEmpty(gamePath))
            {
                Console.WriteLine(" No valid game path provided. Exiting...");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
                return;
            }

            Console.WriteLine($"Using game path: {gamePath}");
            Console.WriteLine();

            // Verify the path exists and contains the game
            if (!VerifyGamePath(gamePath))
            {
                Console.WriteLine(" The specified path doesn't appear to contain Whisper Mountain Outbreak.");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
                return;
            }
            // Generate asset list for user reference
            Console.WriteLine();
      
            // Confirm before patching
            Console.WriteLine("Ready to start patching. This will modify game files.");
            Console.Write("Continue? (Y/N): ");
            
            var response = Console.ReadKey().KeyChar;
            Console.WriteLine();
            Console.WriteLine();
            
            if (char.ToUpper(response) != 'Y')
            {
                Console.WriteLine("Patching cancelled.");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
                return;
            }

            // Start patching process
            Console.WriteLine("Starting patching process...");
            Console.WriteLine();

            bool success = WMO.AssetPatcher.AssetPatcher.TryPatch(DEFAULT_GAME_PATH);

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
            Console.WriteLine("Press any key to exit...");
           Console.ReadKey();
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, $"Unhandled exception in main: {ex}");
            Console.WriteLine();
            Console.WriteLine($" Fatal error: {ex.Message}");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
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
        Console.WriteLine($"Default path: {DEFAULT_GAME_PATH}");
        Console.WriteLine();
        Console.Write("Use the default path? (Y/N): ");
        
        var useDefault = char.ToUpper(Console.ReadKey().KeyChar) == 'Y';
        Console.WriteLine();
        Console.WriteLine();

        return useDefault ? DEFAULT_GAME_PATH : string.Empty;

        
        Console.WriteLine("Please enter the path to your game's data directory:");
        Console.WriteLine("(This should end with 'Whisper Mountain Outbreak_Data')");
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
