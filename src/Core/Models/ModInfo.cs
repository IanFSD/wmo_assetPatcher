using System.ComponentModel;

namespace WMO.Core.Models;

/// <summary>
/// Enhanced mod information model for UI binding
/// </summary>
public class ModInfo : INotifyPropertyChanged
{
    private bool _isEnabled = true;
    private string _status = "Ready";
    
    public required string Name { get; init; }
    public required string FilePath { get; init; }
    public required ModType Type { get; init; }
    public required long FileSize { get; init; }
    public string? Description { get; init; }
    public string? Version { get; init; }
    public string? Author { get; init; }
    public DateTime? CreatedDate { get; init; }
    public DateTime? ModifiedDate { get; init; }
    
    /// <summary>
    /// Whether this mod is enabled for patching
    /// </summary>
    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }
    
    /// <summary>
    /// Current status of the mod (Ready, Patching, Complete, Error)
    /// </summary>
    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }
    
    /// <summary>
    /// Formatted file size for display
    /// </summary>
    public string FormattedFileSize => FormatFileSize(FileSize);
    
    /// <summary>
    /// Gets the mod type as a display string
    /// </summary>
    public string TypeDisplay => Type switch
    {
        ModType.Audio => "🎵 Audio",
        ModType.Sprite => "🖼️ Sprite", 
        ModType.Texture => "🎨 Texture",
        _ => "❓ Unknown"
    };
    
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
    
    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}

/// <summary>
/// Mod file types supported by the patcher
/// </summary>
public enum ModType
{
    Audio,
    Sprite, 
    Texture
}
