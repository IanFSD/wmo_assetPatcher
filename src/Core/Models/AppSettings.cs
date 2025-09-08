using System.ComponentModel;
using WMO.Core.Logging;

namespace WMO.Core.Models;

/// <summary>
/// Application settings for the UI version
/// </summary>
public class AppSettings : INotifyPropertyChanged
{
    private string? _gamePath;
    private bool _minimizeToTray = false;
    private LogLevel _logLevel = LogLevel.Info;
    private bool _darkMode = false;
    private int _windowWidth = 800;
    private int _windowHeight = 600;
    private bool _rememberWindowSize = true;
    
    // Legacy console settings properties
    private bool _allowStartupWithConflicts = false;
    private bool _isPatched = false;
    
    /// <summary>
    /// Path to the game installation directory
    /// </summary>
    public string? GamePath
    {
        get => _gamePath;
        set => SetProperty(ref _gamePath, value);
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
    
    /// <summary>
    /// Whether to allow startup even when there are conflicts (legacy console setting)
    /// </summary>
    public bool AllowStartupWithConflicts
    {
        get => _allowStartupWithConflicts;
        set => SetProperty(ref _allowStartupWithConflicts, value);
    }
    
    /// <summary>
    /// Whether the game has been patched (legacy console setting)
    /// </summary>
    public bool IsPatched
    {
        get => _isPatched;
        set => SetProperty(ref _isPatched, value);
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
