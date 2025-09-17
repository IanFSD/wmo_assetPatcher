using System.ComponentModel;
using System.Collections.ObjectModel;
using WMO.Core.Models;

namespace WMO.UI.ViewModels;

/// <summary>
/// View model for folder-based mods with UI binding support
/// </summary>
public class FolderModViewModel : INotifyPropertyChanged
{
    private readonly FolderMod _folderMod;
    private bool _isEnabled = true;
    private string _status = "Ready";
    
    public FolderModViewModel(FolderMod folderMod)
    {
        _folderMod = folderMod ?? throw new ArgumentNullException(nameof(folderMod));
        ModFiles = new ObservableCollection<ModFileViewModel>(
            folderMod.ModFiles.Select(mf => new ModFileViewModel(mf))
        );
    }
    
    // Expose core model properties
    public string Name => _folderMod.Name;
    public string FolderPath => _folderMod.FolderPath;
    public string? Description => _folderMod.Description;
    public string? Version => _folderMod.Version;
    public string? Author => _folderMod.Author;
    public DateTime? CreatedDate => _folderMod.CreatedDate;
    public DateTime? ModifiedDate => _folderMod.ModifiedDate;
    
    // UI-specific properties
    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }
    
    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }
    
    public ObservableCollection<ModFileViewModel> ModFiles { get; }
    
    // Computed properties
    public int TotalFiles => ModFiles.Count;
    public int EnabledFiles => ModFiles.Count(f => f.IsEnabled);
    public string DisplayName => $"{Name} ({EnabledFiles}/{TotalFiles} files)";
    public long TotalSize => ModFiles.Sum(f => f.FileSize);
    public string FormattedTotalSize => FormatFileSize(TotalSize);
    
    // Access to underlying model
    public FolderMod CoreModel => _folderMod;
    
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
