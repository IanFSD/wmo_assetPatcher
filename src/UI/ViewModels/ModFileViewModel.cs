using System.ComponentModel;
using WMO.Core.Models;
using WMO.Core.Models.Enums;

namespace WMO.UI.ViewModels;

/// <summary>
/// View model for individual mod files with UI binding support
/// </summary>
public class ModFileViewModel : INotifyPropertyChanged
{
    private readonly ModFile _modFile;
    private bool _isEnabled = true;
    
    public ModFileViewModel(ModFile modFile)
    {
        _modFile = modFile ?? throw new ArgumentNullException(nameof(modFile));
        _isEnabled = modFile.IsEnabled;
        
        // Subscribe to core model changes if it implements INotifyPropertyChanged
        if (_modFile is INotifyPropertyChanged notifyPropertyChanged)
        {
            notifyPropertyChanged.PropertyChanged += OnCoreModelPropertyChanged;
        }
    }
    
    // Expose core model properties
    public string Name => _modFile.Name;
    public string FilePath => _modFile.FilePath;
    public ModType Type => _modFile.Type;
    public long FileSize => _modFile.FileSize;
    public DateTime? CreatedDate => _modFile.CreatedDate;
    public DateTime? ModifiedDate => _modFile.ModifiedDate;
    public string FormattedFileSize => _modFile.FormattedFileSize;
    public string TypeDescription => _modFile.TypeDescription;
    public string StatusText => _modFile.StatusText;
    
    // UI-specific enabled state
    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (SetProperty(ref _isEnabled, value))
            {
                // Update the core model
                _modFile.IsEnabled = value;
            }
        }
    }
    
    // Access to underlying model
    public ModFile CoreModel => _modFile;
    
    private void OnCoreModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Forward relevant property changes from core model
        switch (e.PropertyName)
        {
            case nameof(ModFile.IsEnabled):
                if (_modFile.IsEnabled != _isEnabled)
                {
                    _isEnabled = _modFile.IsEnabled;
                    OnPropertyChanged(nameof(IsEnabled));
                }
                break;
        }
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
