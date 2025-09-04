using WMO.Core.Helpers;

namespace WMO.UI.Models;

/// <summary>
/// Represents a mod item for display in the UI
/// </summary>
public class ModItem
{
    public string Name { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public ModType Type { get; set; }
    public bool IsSelected { get; set; } = true;
    public string DisplayName => $"{Name} ({Type})";
    
    public ModItem() { }
    
    public ModItem(AudioMod audioMod)
    {
        Name = audioMod.AssetName;
        FilePath = audioMod.FilePath;
        Type = ModType.Audio;
    }
    
    public ModItem(SpriteMod spriteMod)
    {
        Name = spriteMod.AssetName;
        FilePath = spriteMod.FilePath;
        Type = ModType.Sprite;
    }
    
    public ModItem(TextureMod textureMod)
    {
        Name = textureMod.AssetName;
        FilePath = textureMod.FilePath;
        Type = ModType.Texture;
    }
}

/// <summary>
/// Type of mod
/// </summary>
public enum ModType
{
    Audio,
    Sprite,
    Texture
}
