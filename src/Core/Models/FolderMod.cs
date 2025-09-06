using System.ComponentModel;
using System.Collections.ObjectModel;

namespace WMO.Core.Models;

/// <summary>
/// Represents a mod that is organized as a folder containing multiple mod files
/// </summary>
public class FolderMod : INotifyPropertyChanged
{
    private bool _isEnabled = true;
    private string _status = "Ready";
    
    public required string Name { get; init; }
    public required string FolderPath { get; init; }
    public string? Description { get; set; }
    public string? Version { get; set; }
    public string? Author { get; set; }
    public DateTime? CreatedDate { get; init; }
    public DateTime? ModifiedDate { get; init; }
    
    /// <summary>
    /// Collection of mod files contained in this mod folder
    /// </summary>
    public ObservableCollection<ModFile> ModFiles { get; } = new();
    
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
    /// Gets the total number of mod files
    /// </summary>
    public int FileCount => ModFiles.Count;
    
    /// <summary>
    /// Gets the total size of all mod files
    /// </summary>
    public long TotalSize => ModFiles.Sum(f => f.FileSize);
    
    /// <summary>
    /// Gets the formatted total size for display
    /// </summary>
    public string FormattedTotalSize => FormatFileSize(TotalSize);
    
    /// <summary>
    /// Gets a summary of mod types contained in this mod
    /// </summary>
    public string TypesSummary
    {
        get
        {
            var types = ModFiles.GroupBy(f => f.Type)
                               .Select(g => $"{g.Count()} {g.Key}")
                               .ToArray();
            return string.Join(", ", types);
        }
    }
    
    /// <summary>
    /// Gets count of each mod type
    /// </summary>
    public int AudioFileCount => ModFiles.Count(f => f.Type == ModType.Audio);
    public int SpriteFileCount => ModFiles.Count(f => f.Type == ModType.Sprite);
    public int TextureFileCount => ModFiles.Count(f => f.Type == ModType.Texture);
    
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
