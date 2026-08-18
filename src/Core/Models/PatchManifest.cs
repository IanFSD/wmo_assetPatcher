using System.Text.Json.Serialization;

namespace WMO.Core.Models;

/// <summary>
/// Tracks which game asset files were patched in the current session so they
/// can be fully restored when the game exits (or on the next patcher launch
/// if the previous session ended without a clean restore).
///
/// Serialized as JSON to <AppDir>/backups/patch_manifest.json.
/// </summary>
public class PatchManifest
{
    /// <summary>
    /// UTC timestamp when this manifest was written.
    /// </summary>
    public DateTime PatchedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// One entry per game file that was modified.
    /// A single mod audio replacement may touch both a .assets file and its
    /// sibling .resS file - each becomes its own entry here.
    /// </summary>
    public List<PatchedFile> Files { get; set; } = [];

    /// <summary>
    /// Returns true when there is at least one file to restore.
    /// </summary>
    [JsonIgnore]
    public bool HasEntries => Files.Count > 0;
}

/// <summary>
/// Maps one patched game file to its backup copy.
/// </summary>
public class PatchedFile
{
    /// <summary>
    /// Absolute path to the game file that was modified (e.g. -\sharedassets2.assets).
    /// </summary>
    public required string OriginalPath { get; set; }

    /// <summary>
    /// Absolute path to the backup copy stored in the patcher's backups/ folder
    /// (e.g. -\backups\sharedassets2.assets.wmo_bak).
    /// </summary>
    public required string BackupPath { get; set; }
}
