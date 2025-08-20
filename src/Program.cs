using System.Diagnostics;
using System.Security.Principal;
using WMO.AssetPatcher;
using WMO.Helper;
using WMO.Logging;

namespace WMO;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
       Logger.Log(LogLevel.Trace, $"Starting WMO...");
        SettingsSaver.LoadSettings();

        // Define paths
        var gameDataPath = @"C:\Program Files (x86)\Steam\steamapps\common\Whisper Mountain Outbreak\Whisper Mountain Outbreak_Data";
        var assetsFilePath = Directory.GetFiles(gameDataPath, "*.assets");

        // Patch audio assets
        Logger.Log(LogLevel.Info, $"Patching audio assets in: {assetsFilePath}");   
    }
}