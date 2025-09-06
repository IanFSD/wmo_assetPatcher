using System.ComponentModel;

namespace WMO.Core.Models;

/// <summary>
/// Represents an individual mod file within a folder mod
/// </summary>
public class ModFile : INotifyPropertyChanged
{
    private bool _isEnabled = true;
    
    public required string Name { get; init; }
    public required string FilePath { get; init; }
    public required ModType Type { get; init; }
    public required long FileSize { get; init; }
    public DateTime? CreatedDate { get; init; }
    public DateTime? ModifiedDate { get; init; }
    
    /// <summary>
    /// Whether this individual file is enabled for patching
    /// </summary>
    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
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
    
    /// <summary>
    /// Gets just the filename without path
    /// </summary>
    public string FileName => Path.GetFileName(FilePath);
    
    /// <summary>
    /// Gets the file extension
    /// </summary>
    public string Extension => Path.GetExtension(FilePath);
    
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
