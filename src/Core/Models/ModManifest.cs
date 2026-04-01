using System.Text.Json.Serialization;

namespace WMO.Core.Models;

/// <summary>
/// Represents a mod manifest file (manifest.json) - similar to SMAPI's manifest system
/// </summary>
public class ModManifest
{
    /// <summary>
    /// The mod's display name
    /// </summary>
    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// A brief description of the mod
    /// </summary>
    [JsonPropertyName("Description")]
    public string? Description { get; set; }
    
    /// <summary>
    /// The mod author's name
    /// </summary>
    [JsonPropertyName("Author")]
    public string? Author { get; set; }
    
    /// <summary>
    /// The mod version in semantic versioning format (e.g., "1.0.0")
    /// </summary>
    [JsonPropertyName("Version")]
    public string Version { get; set; } = "1.0.0";
    
    /// <summary>
    /// Unique identifier for the mod (e.g., "AuthorName.ModName")
    /// </summary>
    [JsonPropertyName("UniqueID")]
    public string UniqueID { get; set; } = string.Empty;
    
    /// <summary>
    /// Minimum required patcher version
    /// </summary>
    [JsonPropertyName("MinimumPatcherVersion")]
    public string? MinimumPatcherVersion { get; set; }
    
    /// <summary>
    /// Update keys for checking for new versions (optional)
    /// </summary>
    [JsonPropertyName("UpdateKeys")]
    public List<string>? UpdateKeys { get; set; }
    
    /// <summary>
    /// Dependencies required for this mod to work
    /// </summary>
    [JsonPropertyName("Dependencies")]
    public List<ModDependency>? Dependencies { get; set; }
    
    /// <summary>
    /// Content files to be patched (audio, sprites, textures, BepInEx plugins)
    /// </summary>
    [JsonPropertyName("ContentFiles")]
    public List<ContentFile>? ContentFiles { get; set; }
    
    /// <summary>
    /// Validates the manifest for required fields
    /// </summary>
    public (bool IsValid, string ErrorMessage) Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
            return (false, "Manifest missing required field: Name");
            
        if (string.IsNullOrWhiteSpace(UniqueID))
            return (false, "Manifest missing required field: UniqueID");
            
        if (string.IsNullOrWhiteSpace(Version))
            return (false, "Manifest missing required field: Version");
            
        // Validate UniqueID format (should not contain spaces or special characters except dots and hyphens)
        if (UniqueID.Any(c => !char.IsLetterOrDigit(c) && c != '.' && c != '-' && c != '_'))
            return (false, $"UniqueID '{UniqueID}' contains invalid characters. Use only letters, numbers, dots, hyphens, and underscores.");
            
        return (true, string.Empty);
    }
}

/// <summary>
/// Represents a dependency on another mod
/// </summary>
public class ModDependency
{
    /// <summary>
    /// The unique ID of the required mod
    /// </summary>
    [JsonPropertyName("UniqueID")]
    public string UniqueID { get; set; } = string.Empty;
    
    /// <summary>
    /// Minimum version of the dependency
    /// </summary>
    [JsonPropertyName("MinimumVersion")]
    public string? MinimumVersion { get; set; }
    
    /// <summary>
    /// Whether this dependency is required (true) or optional (false)
    /// </summary>
    [JsonPropertyName("IsRequired")]
    public bool IsRequired { get; set; } = true;
}

/// <summary>
/// Represents a content file to be patched
/// </summary>
public class ContentFile
{
    /// <summary>
    /// Path to the file relative to the mod folder
    /// </summary>
    [JsonPropertyName("FilePath")]
    public string FilePath { get; set; } = string.Empty;
    
    /// <summary>
    /// Target asset name in the game to replace
    /// Optional for BepInEx plugins (they're copied to plugins folder)
    /// </summary>
    [JsonPropertyName("Target")]
    public string? Target { get; set; }
    
    /// <summary>
    /// Type of asset (Audio, Sprite, Texture, BepInExPlugin)
    /// If not specified, will be auto-detected from file extension
    /// </summary>
    [JsonPropertyName("Type")]
    public string? Type { get; set; }
    
    /// <summary>
    /// Whether this file is enabled (can be toggled by users)
    /// </summary>
    [JsonPropertyName("Enabled")]
    public bool Enabled { get; set; } = true;
}
