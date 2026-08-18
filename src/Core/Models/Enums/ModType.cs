namespace WMO.Core.Models.Enums;

/// <summary>
/// Mod file types supported by the patcher
/// </summary>
public enum ModType
{
    Audio,
    Sprite, 
    Texture,
    BepInExPlugin,
    /// <summary>Overwrite fields on an existing MonoBehaviour/ScriptableObject asset.</summary>
    MonoBehaviourEdit,
    /// <summary>Append a new MonoBehaviour/ScriptableObject asset into an existing .assets file.</summary>
    MonoBehaviourInject
}
