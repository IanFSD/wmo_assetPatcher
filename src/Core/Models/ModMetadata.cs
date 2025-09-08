using WMO.Core.Models.Enums;

namespace WMO.Core.Models;

/// <summary>
/// Pure metadata model for mod information (no UI concerns)
/// </summary>
public class ModMetadata
{
    public string Name { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public ModType Type { get; set; }
    public long FileSize { get; set; }
    public string? Description { get; set; }
    public string? Version { get; set; }
    public string? Author { get; set; }
    public DateTime? CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    
    /// <summary>
    /// Whether this mod is enabled for use (service layer concern)
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
        ModType.Audio => "Audio Replacement",
        ModType.Sprite => "Sprite Replacement", 
        ModType.Texture => "Texture Replacement",
        _ => "Unknown Type"
    };
    
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
