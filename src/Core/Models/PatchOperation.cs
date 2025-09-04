namespace WMO.Core.Models;

/// <summary>
/// Represents a patching operation with progress tracking
/// </summary>
public class PatchOperation
{
    public required string FileName { get; init; }
    public required string FullPath { get; init; }
    public required int TotalMods { get; init; }
    public int ProcessedMods { get; set; }
    public int SkippedMods { get; set; }
    public PatchStatus Status { get; set; } = PatchStatus.Pending;
    public string? ErrorMessage { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    
    /// <summary>
    /// Progress percentage (0-100)
    /// </summary>
    public double ProgressPercentage => TotalMods > 0 ? (double)ProcessedMods / TotalMods * 100 : 0;
    
    /// <summary>
    /// Duration of the operation
    /// </summary>
    public TimeSpan? Duration => EndTime.HasValue && StartTime.HasValue ? EndTime.Value - StartTime.Value : null;
    
    /// <summary>
    /// Status message for display
    /// </summary>
    public string StatusMessage => Status switch
    {
        PatchStatus.Pending => "Waiting to start...",
        PatchStatus.InProgress => $"Processing {FileName}... ({ProcessedMods}/{TotalMods})",
        PatchStatus.Completed => $"Completed ({ProcessedMods} mods applied, {SkippedMods} skipped)",
        PatchStatus.Failed => $"Failed: {ErrorMessage}",
        PatchStatus.Cancelled => "Cancelled",
        _ => "Unknown status"
    };
}

/// <summary>
/// Overall progress information for the entire patching process
/// </summary>
public class PatchProgress
{
    public required int TotalFiles { get; init; }
    public int ProcessedFiles { get; set; }
    public int TotalAssets { get; set; }
    public int ProcessedAssets { get; set; }
    public string? CurrentOperation { get; set; }
    public string? CurrentFile { get; set; }
    public PatchOperation? CurrentFileOperation { get; set; }
    
    /// <summary>
    /// Overall progress percentage (0-100)
    /// </summary>
    public double Percentage => TotalFiles > 0 ? (double)ProcessedFiles / TotalFiles * 100 : 0;
    
    /// <summary>
    /// Detailed progress message
    /// </summary>
    public string Message => CurrentFileOperation?.StatusMessage ?? CurrentOperation ?? "Initializing...";
}

/// <summary>
/// Status of a patch operation
/// </summary>
public enum PatchStatus
{
    Pending,
    InProgress,
    Completed,
    Failed,
    Cancelled
}
