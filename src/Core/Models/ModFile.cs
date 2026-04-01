using WMO.Core.Models.Enums;

namespace WMO.Core.Models;

/// <summary>
/// Represents an individual mod file within a folder mod
/// </summary>
public class ModFile
{
    public required string Name { get; init; }
    public required string FilePath { get; init; }
    public required ModType Type { get; init; }
    public required long FileSize { get; init; }
    public DateTime? CreatedDate { get; init; }
    public DateTime? ModifiedDate { get; init; }
    
    /// <summary>
    /// Whether this individual file is enabled for patching
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    
    /// <summary>
    /// Formatted file size for display
    /// </summary>
    public string FormattedFileSize => FormatFileSize(FileSize);
    
    /// <summary>
    /// Human-readable description of the mod type
    /// </summary>
    public string TypeDescription => Type switch
    {
        ModType.Audio => "Audio",
        ModType.Sprite => "Sprite", 
        ModType.Texture => "Texture",
        ModType.BepInExPlugin => "BepInEx Plugin",
        _ => "Unknown"
    };
    
    /// <summary>
    /// Status text for display
    /// </summary>
    public string StatusText => IsEnabled ? "Ready" : "Disabled";
    
    /// <summary>
    /// Gets just the filename without path
    /// </summary>
    public string FileName => Path.GetFileName(FilePath);
    
    /// <summary>
    /// Gets the file extension
    /// </summary>
    public string Extension => Path.GetExtension(FilePath);
    
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
