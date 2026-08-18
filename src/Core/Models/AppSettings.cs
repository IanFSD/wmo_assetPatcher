using System.ComponentModel;
using WMO.Core.Models.Enums;

namespace WMO.Core.Models;

/// <summary>
/// Application settings for the UI version
/// </summary>
public class AppSettings : INotifyPropertyChanged
{
    private string? _gamePath;
    private bool _minimizeToTray = false;
    private bool _showConsole = false;
    private bool _darkMode = false;
    private int _windowWidth = 800;
    private int _windowHeight = 600;
    private bool _rememberWindowSize = true;
    
    // Game version settings
    private GameVersion _gameVersion = GameVersion.FullGame;
    
    // BepInEx installation status
    private bool _bepInExInstalled = false;
    
    // Mod enable/disable state persistence
    private HashSet<string> _disabledModIds = new();
    
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
    /// Whether to show a debug console window
    /// </summary>
    public bool ShowConsole
    {
        get => _showConsole;
        set => SetProperty(ref _showConsole, value);
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
    /// Version of the game (Full Game or Friend's Pass)
    /// </summary>
    public GameVersion GameVersion
    {
        get => _gameVersion;
        set => SetProperty(ref _gameVersion, value);
    }
    
    /// <summary>
    /// Steam Application ID based on game version
    /// Full Game: 1953230, Friend's Pass: 2595010
    /// </summary>
    public string SteamAppId => GameVersion switch
    {
        GameVersion.FullGame => "1953230",      // Full version of Whisper Mountain Outbreak
        GameVersion.FriendsPass => "2595010",  // Friend's Pass demo version
        _ => "1953230" // Default to full game
    };
    
    /// <summary>
    /// Whether BepInEx is installed in the game directory
    /// </summary>
    public bool BepInExInstalled
    {
        get => _bepInExInstalled;
        set => SetProperty(ref _bepInExInstalled, value);
    }
    
    /// <summary>
    /// Set of mod UniqueIDs that the user has explicitly disabled.
    /// All mods not in this set are considered enabled.
    /// </summary>
    public HashSet<string> DisabledModIds
    {
        get => _disabledModIds;
        set => SetProperty(ref _disabledModIds, value);
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
