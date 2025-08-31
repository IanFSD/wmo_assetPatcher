using System.ComponentModel;
using WMO.Core.Logging;

namespace WMO.Core.Models;

/// <summary>
/// Application settings for the UI version
/// </summary>
public class AppSettings : INotifyPropertyChanged
{
    private string? _gamePath;
    private bool _checkForUpdatesOnStartup = true;
    private bool _minimizeToTray = false;
    private bool _autoBackup = true;
    private bool _confirmBeforePatching = true;
    private LogLevel _logLevel = LogLevel.Info;
    private bool _darkMode = false;
    private int _windowWidth = 800;
    private int _windowHeight = 600;
    private bool _rememberWindowSize = true;
    
    /// <summary>
    /// Path to the game installation directory
    /// </summary>
    public string? GamePath
    {
        get => _gamePath;
        set => SetProperty(ref _gamePath, value);
    }
    
    /// <summary>
    /// Whether to check for updates on startup
    /// </summary>
    public bool CheckForUpdatesOnStartup
    {
        get => _checkForUpdatesOnStartup;
        set => SetProperty(ref _checkForUpdatesOnStartup, value);
    }
    
    /// <summary>
    /// Whether to minimize to system tray
    /// </summary>
    public bool MinimizeToTray
    {
        get => _minimizeToTray;
        set => SetProperty(ref _minimizeToTray, value);
    }
    
    /// <summary>
    /// Whether to automatically create backups before patching
    /// </summary>
    public bool AutoBackup
    {
        get => _autoBackup;
        set => SetProperty(ref _autoBackup, value);
    }
    
    /// <summary>
    /// Whether to show confirmation dialog before patching
    /// </summary>
    public bool ConfirmBeforePatching
    {
        get => _confirmBeforePatching;
        set => SetProperty(ref _confirmBeforePatching, value);
    }
    
    /// <summary>
    /// Current logging level
    /// </summary>
    public LogLevel LogLevel
    {
        get => _logLevel;
        set => SetProperty(ref _logLevel, value);
    }
    
    /// <summary>
    /// Whether to use dark mode theme
    /// </summary>
    public bool DarkMode
    {
        get => _darkMode;
        set => SetProperty(ref _darkMode, value);
    }
    
    /// <summary>
    /// Main window width
    /// </summary>
    public int WindowWidth
    {
        get => _windowWidth;
        set => SetProperty(ref _windowWidth, value);
    }
    
    /// <summary>
    /// Main window height
    /// </summary>
    public int WindowHeight
    {
        get => _windowHeight;
        set => SetProperty(ref _windowHeight, value);
    }
    
    /// <summary>
    /// Whether to remember window size between sessions
    /// </summary>
    public bool RememberWindowSize
    {
        get => _rememberWindowSize;
        set => SetProperty(ref _rememberWindowSize, value);
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    
    protected bool SetProperty<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
