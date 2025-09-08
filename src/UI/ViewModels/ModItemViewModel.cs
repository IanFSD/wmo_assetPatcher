using System.ComponentModel;
using WMO.Core.Helpers;
using WMO.Core.Models.Enums;

namespace WMO.UI.ViewModels;

/// <summary>
/// View model for displaying mod items in the UI with proper data binding support
/// </summary>
public class ModItemViewModel : INotifyPropertyChanged
{
    private string _name = string.Empty;
    private string _filePath = string.Empty;
    private ModType _type;
    private bool _isSelected = true;
    
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }
    
    public string FilePath
    {
        get => _filePath;
        set => SetProperty(ref _filePath, value);
    }
    
    public ModType Type
    {
        get => _type;
        set => SetProperty(ref _type, value);
    }
    
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
    
    public string DisplayName => $"{Name} ({Type})";
    
    public ModItemViewModel() { }
    
    public ModItemViewModel(AudioMod audioMod)
    {
        Name = audioMod.AssetName;
        FilePath = audioMod.FilePath;
        Type = ModType.Audio;
    }
    
    public ModItemViewModel(SpriteMod spriteMod)
    {
        Name = spriteMod.AssetName;
        FilePath = spriteMod.FilePath;
        Type = ModType.Sprite;
    }
    
    public ModItemViewModel(TextureMod textureMod)
    {
        Name = textureMod.AssetName;
        FilePath = textureMod.FilePath;
        Type = ModType.Texture;
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
