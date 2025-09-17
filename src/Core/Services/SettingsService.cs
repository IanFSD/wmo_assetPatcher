using System.ComponentModel;
using System.Text.Json;
using WMO.Core.Logging;
using WMO.Core.Models;

namespace WMO.Core.Services;

/// <summary>
/// Service for managing application settings (unified settings system)
/// </summary>
public static class SettingsService
{
    public const string DEFAULT_GAME_PATH = @"C:\Program Files (x86)\Steam\steamapps\common\Whisper Mountain Outbreak";
    
    private static readonly string SettingsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings");
    private static readonly string SettingsPath = Path.Combine(SettingsDirectory, "settings.json");
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    
    private static AppSettings? _currentSettings;
    
    /// <summary>
    /// Gets the current settings instance
    /// </summary>
    public static AppSettings Current => _currentSettings ??= LoadSettings();
    
    /// <summary>
    /// Loads settings from the settings file, with migration support for legacy files
    /// </summary>
    /// <returns>The loaded settings or default settings if file doesn't exist</returns>
    public static AppSettings LoadSettings()
    {
        try
        {
            // Try to load from the main settings.json first
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json, Options);
                if (settings != null)
                {
                    _currentSettings = settings;
                    
                    // Subscribe to property changes for auto-save
                    settings.PropertyChanged += OnSettingsChanged;
                    
                    return settings;
                }
            }
            
            // No settings file found, using defaults
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading settings: {ex.Message}"); // Use Console to avoid circular dependency
        }
        
        // Return default settings
        var defaultSettings = new AppSettings();
        _currentSettings = defaultSettings;
        defaultSettings.PropertyChanged += OnSettingsChanged;
        return defaultSettings;
    }
    
    /// <summary>
    /// Saves settings to the settings file
    /// </summary>
    /// <param name="settings">Settings to save</param>
    public static void SaveSettings(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(SettingsDirectory);
            
            var json = JsonSerializer.Serialize(settings, Options);
            File.WriteAllText(SettingsPath, json);
            
            Logger.Log(LogLevel.Debug, $"Settings saved to: {SettingsPath}");
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, $"Error saving settings: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Checks if this is the first time the application is running
    /// (i.e., no settings file exists)
    /// </summary>
    /// <returns>True if this is the first run</returns>
    public static bool IsFirstRun() => !File.Exists(SettingsPath);
    
    /// <summary>
    /// Event handler for when settings properties change - auto-saves settings
    /// </summary>
    private static void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is AppSettings settings)
        {
            SaveSettings(settings);
            Logger.Log(LogLevel.Debug, $"Setting changed: {e.PropertyName}");
        }
    }
}
