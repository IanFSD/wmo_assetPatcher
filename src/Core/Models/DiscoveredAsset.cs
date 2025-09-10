using WMO.Core.Models.Enums;

namespace WMO.Core.Models;

/// <summary>
/// Represents a Unity asset discovered during scanning
/// </summary>
public class DiscoveredAsset
{
    /// <summary>
    /// Unique identifier for this asset
    /// </summary>
    public long PathId { get; set; }
    
    /// <summary>
    /// Name of the asset
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Type of the asset
    /// </summary>
    public UnityAssetType AssetType { get; set; }
    
    /// <summary>
    /// Asset class ID from Unity
    /// </summary>
    public int ClassId { get; set; }
    
    /// <summary>
    /// Size of the asset data in bytes
    /// </summary>
    public long Size { get; set; }
    
    /// <summary>
    /// File path where this asset is located
    /// </summary>
    public string FilePath { get; set; } = string.Empty;
    
    /// <summary>
    /// File name where this asset is located
    /// </summary>
    public string FileName => Path.GetFileName(FilePath);
    
    /// <summary>
    /// Formatted size string for display
    /// </summary>
    public string FormattedSize
    {
        get
        {
            if (Size >= 1024 * 1024 * 1024) // GB
                return $"{Size / (1024.0 * 1024.0 * 1024.0):F2} GB";
            if (Size >= 1024 * 1024) // MB
                return $"{Size / (1024.0 * 1024.0):F2} MB";
            if (Size >= 1024) // KB
                return $"{Size / 1024.0:F2} KB";
            return $"{Size} B";
        }
    }
    
    /// <summary>
    /// Display name for the asset type
    /// </summary>
    public string AssetTypeName => AssetType.GetDisplayName();
    
    /// <summary>
    /// Additional properties or metadata
    /// </summary>
    public Dictionary<string, object> Properties { get; set; } = new();
    
    /// <summary>
    /// Whether this asset can potentially be modded
    /// </summary>
    public bool IsModdable => UnityAssetTypeExtensions.GetModdableTypes().Contains(AssetType);
    
    public override string ToString()
    {
        return $"{Name} ({AssetTypeName}) - {FormattedSize} in {FileName}";
    }
}

/// <summary>
/// Filter options for asset scanning
/// </summary>
public class AssetScanFilter
{
    /// <summary>
    /// Asset types to include in the scan
    /// </summary>
    public HashSet<UnityAssetType> IncludedTypes { get; set; } = new();
    
    /// <summary>
    /// Minimum asset size in bytes (0 = no limit)
    /// </summary>
    public long MinSize { get; set; } = 0;
    
    /// <summary>
    /// Maximum asset size in bytes (0 = no limit)
    /// </summary>
    public long MaxSize { get; set; } = 0;
    
    /// <summary>
    /// Name pattern filter (supports wildcards)
    /// </summary>
    public string? NameFilter { get; set; }
    
    /// <summary>
    /// Whether to include only moddable asset types
    /// </summary>
    public bool ModdableOnly { get; set; } = false;
    
    /// <summary>
    /// Whether to include empty/zero-size assets
    /// </summary>
    public bool IncludeEmpty { get; set; } = true;
    
    /// <summary>
    /// Check if an asset passes this filter
    /// </summary>
    public bool PassesFilter(DiscoveredAsset asset)
    {
        // Type filter
        if (IncludedTypes.Count > 0 && !IncludedTypes.Contains(asset.AssetType))
            return false;
            
        // Moddable only filter
        if (ModdableOnly && !asset.IsModdable)
            return false;
            
        // Size filters
        if (MinSize > 0 && asset.Size < MinSize)
            return false;
            
        if (MaxSize > 0 && asset.Size > MaxSize)
            return false;
            
        // Empty assets filter
        if (!IncludeEmpty && asset.Size == 0)
            return false;
            
        // Name filter
        if (!string.IsNullOrEmpty(NameFilter))
        {
            var pattern = NameFilter.Replace("*", ".*").Replace("?", ".");
            if (!System.Text.RegularExpressions.Regex.IsMatch(asset.Name, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Create a filter for commonly moddable assets
    /// </summary>
    public static AssetScanFilter CreateModdableFilter()
    {
        return new AssetScanFilter
        {
            IncludedTypes = UnityAssetTypeExtensions.GetModdableTypes().ToHashSet(),
            MinSize = 0,
            IncludeEmpty = false
        };
    }
    
    /// <summary>
    /// Create a filter for all asset types
    /// </summary>
    public static AssetScanFilter CreateAllTypesFilter()
    {
        return new AssetScanFilter
        {
            IncludedTypes = Enum.GetValues<UnityAssetType>().ToHashSet(),
            IncludeEmpty = true
        };
    }
}
