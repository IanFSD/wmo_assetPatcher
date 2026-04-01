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
        
        try
        {
            Logger.Log(LogLevel.Info, $"=== WMO Mod Loader Started ===");

            RunUIMode();
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Fatal, $"Unhandled exception in main: {ex}");
            MessageBox.Show($"Fatal error: {ex.Message}", "Fatal Error", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
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
}
