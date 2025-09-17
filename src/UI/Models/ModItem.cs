using WMO.Core.Helpers;
using WMO.Core.Models.Enums;

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
    
    
    public ModItem(AudioAsset audioMod)
    {
        Name = audioMod.AssetName;
        FilePath = audioMod.FilePath;
        Type = ModType.Audio;
    }
    
    public ModItem(SpriteAsset spriteMod)
    {
        Name = spriteMod.AssetName;
        FilePath = spriteMod.FilePath;
        Type = ModType.Sprite;
    }
    
    public ModItem(TextureAsset textureMod)
    {
        Name = textureMod.AssetName;
        FilePath = textureMod.FilePath;
        Type = ModType.Texture;
    }
}
