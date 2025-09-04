using System.Text.Json;
using WMO.Core.Logging;
using WMO.Core.Models;

namespace WMO.Core.Services;

/// <summary>
/// Service for managing UI application settings
/// </summary>
public static class UISettingsService
{
    private static readonly string SettingsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings");
    private static readonly string UISettingsPath = Path.Combine(SettingsDirectory, "ui_settings.json");
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    
    private static AppSettings? _currentSettings;
    
    /// <summary>
    /// Gets the current UI settings instance
    /// </summary>
    public static AppSettings Current => _currentSettings ??= LoadSettings();
    
    /// <summary>
    /// Loads UI settings from the settings file
    /// </summary>
    /// <returns>The loaded settings or default settings if file doesn't exist</returns>
    public static AppSettings LoadSettings()
    {
        try
        {
            if (File.Exists(UISettingsPath))
            {
                var json = File.ReadAllText(UISettingsPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json, Options);
                if (settings != null)
                {
                    Logger.Log(LogLevel.Info, $"UI settings loaded from: {UISettingsPath}");
                    _currentSettings = settings;
                    
                    // Subscribe to property changes for auto-save
                    settings.PropertyChanged += OnSettingsChanged;
                    
                    return settings;
                }
            }
            
            Logger.Log(LogLevel.Info, $"UI settings file not found, using defaults");
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, $"Error loading UI settings: {ex.Message}");
        }
        
        // Return default settings
        var defaultSettings = new AppSettings();
        _currentSettings = defaultSettings;
        
        // Subscribe to property changes for auto-save
        defaultSettings.PropertyChanged += OnSettingsChanged;
        
        return defaultSettings;
    }
    
    /// <summary>
    /// Saves the current UI settings to file
    /// </summary>
    public static void SaveSettings()
    {
        SaveSettings(Current);
    }
    
    /// <summary>
    /// Saves the specified UI settings to file
    /// </summary>
    /// <param name="settings">Settings to save</param>
    public static void SaveSettings(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(SettingsDirectory);
            
            var json = JsonSerializer.Serialize(settings, Options);
            File.WriteAllText(UISettingsPath, json);
            
            Logger.Log(LogLevel.Debug, $"UI settings saved to: {UISettingsPath}");
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, $"Error saving UI settings: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Checks if this is the first time the application is running
    /// (i.e., no UI settings file exists)
    /// </summary>
    /// <returns>True if this is the first run</returns>
    public static bool IsFirstRun()
    {
        return !File.Exists(UISettingsPath);
    }
    
    /// <summary>
    /// Event handler for settings property changes - auto-saves settings
    /// </summary>
    private static void OnSettingsChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is AppSettings settings)
        {
            Logger.Log(LogLevel.Debug, $"UI setting changed: {e.PropertyName}");
            SaveSettings(settings);
        }
    }
}
