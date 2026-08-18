using System.Collections.ObjectModel;
using WMO.Core.Models;
using WMO.Core.Models.Enums;
using WMO.Core.Logging;
using WMO.Core.Helpers;
using System.Text.Json;

namespace WMO.Core.Services;

/// <summary>
/// Service for managing folder-based mods where each folder represents a mod
/// </summary>
public class FolderModService
{
    private readonly ObservableCollection<FolderMod> _availableMods = new();
    private readonly string _modsDirectory;
    
    public FolderModService()
    {
        _modsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mods");
    }
    
    /// <summary>
    /// Collection of available folder mods
    /// </summary>
    public ObservableCollection<FolderMod> AvailableMods => _availableMods;
    
    /// <summary>
    /// Scans for and loads all available folder mods
    /// </summary>
    public async Task RefreshModsAsync()
    {
        await Task.Run(() =>
        {
            _availableMods.Clear();
            
            if (!Directory.Exists(_modsDirectory))
            {
                Logger.Log(LogLevel.Info, $"Mods directory not found, creating: {_modsDirectory}");
                Directory.CreateDirectory(_modsDirectory);
                return;
            }
            
            var modFolders = Directory.GetDirectories(_modsDirectory);
            Logger.Log(LogLevel.Info, $"Found {modFolders.Length} mod folders in {_modsDirectory}");
            
            foreach (var folderPath in modFolders)
            {
                try
                {
                    var folderMod = ScanModFolder(folderPath);
                    if (folderMod != null)
                    {
                        _availableMods.Add(folderMod);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Error, $"Error scanning mod folder {folderPath}: {ex.Message}");
                }
            }
            
            Logger.Log(LogLevel.Info, $"Loaded {_availableMods.Count} mods with {_availableMods.Sum(m => m.FileCount)} total files");
        });
    }
    
    /// <summary>
    /// Scans a single mod folder and creates a FolderMod instance
    /// Requires manifest.json to be present
    /// </summary>
    private FolderMod? ScanModFolder(string folderPath)
    {
        var folderInfo = new DirectoryInfo(folderPath);
        var folderName = folderInfo.Name;
        
        Logger.Log(LogLevel.Debug, $"Scanning mod folder: {folderName}");
        
        // Check for manifest.json (required)
        var manifestPath = Path.Combine(folderPath, "manifest.json");
        if (!File.Exists(manifestPath))
        {
            Logger.Log(LogLevel.Warning, $"Skipping folder '{folderName}' - manifest.json required");
            return null;
        }
        
        return ScanManifestBasedMod(folderPath, manifestPath, folderInfo);
    }
    

    
    /// <summary>
    /// Creates a new mod folder structure
    /// </summary>
    public async Task<bool> CreateModAsync(string modName, string? description = null, string? author = null, string? version = null)
    {
        try
        {
            if (!Directory.Exists(_modsDirectory))
            {
                Directory.CreateDirectory(_modsDirectory);
            }
            
            var modFolderPath = Path.Combine(_modsDirectory, modName);
            
            if (Directory.Exists(modFolderPath))
            {
                Logger.Log(LogLevel.Warning, $"Mod folder already exists: {modName}");
                return false;
            }
            
            await Task.Run(() =>
            {
                Directory.CreateDirectory(modFolderPath);
                
                // Create mod.json with metadata
                var metadata = new
                {
                    Name = modName,
                    Description = description ?? $"Custom mod: {modName}",
                    Author = author ?? "Unknown",
                    Version = version ?? "1.0.0"
                };
                
                var metadataJson = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(Path.Combine(modFolderPath, "mod.json"), metadataJson);
            });
            
            Logger.Log(LogLevel.Success, $"Created new mod folder: {modName}");
            
            // Refresh the mods list
            await RefreshModsAsync();
            return true;
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, $"Failed to create mod {modName}: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Removes a mod folder and all its contents
    /// </summary>
    public async Task<bool> RemoveModAsync(FolderMod folderMod)
    {
        try
        {
            await Task.Run(() =>
            {
                if (Directory.Exists(folderMod.FolderPath))
                {
                    Directory.Delete(folderMod.FolderPath, true);
                }
            });
            
            _availableMods.Remove(folderMod);
            Logger.Log(LogLevel.Success, $"Removed mod: {folderMod.Name}");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, $"Failed to remove mod {folderMod.Name}: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Opens a mod folder in Windows Explorer
    /// </summary>
    public void OpenModFolder(FolderMod folderMod)
    {
        try
        {
            System.Diagnostics.Process.Start("explorer.exe", folderMod.FolderPath);
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, $"Failed to open mod folder {folderMod.Name}: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Opens the main mods directory in Windows Explorer
    /// </summary>
    public void OpenModsDirectory()
    {
        try
        {
            if (!Directory.Exists(_modsDirectory))
            {
                Directory.CreateDirectory(_modsDirectory);
            }
            
            System.Diagnostics.Process.Start("explorer.exe", _modsDirectory);
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, $"Failed to open mods directory: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Scans a manifest-based mod folder
    /// </summary>
    private FolderMod? ScanManifestBasedMod(string folderPath, string manifestPath, DirectoryInfo folderInfo)
    {
        try
        {
            // Read and parse manifest
            var manifestJson = File.ReadAllText(manifestPath);
            var manifest = JsonSerializer.Deserialize<ModManifest>(manifestJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            });

            if (manifest == null)
            {
                Logger.Log(LogLevel.Warning, $"Failed to parse manifest.json in {folderPath}");
                return null;
            }

            // Validate manifest
            var (isValid, errorMessage) = manifest.Validate();
            if (!isValid)
            {
                Logger.Log(LogLevel.Error, $"Invalid manifest in {folderPath}: {errorMessage}");
                return null;
            }

            Logger.Log(LogLevel.Info, $"Loading manifest-based mod: {manifest.Name} v{manifest.Version}");

            var modFiles = new List<ModFile>();

            // Process content files from manifest
            if (manifest.ContentFiles != null)
            {
                foreach (var contentFile in manifest.ContentFiles)
                {
                    if (!contentFile.Enabled) continue;

                    var filePath = Path.Combine(folderPath, contentFile.FilePath);
                    if (!File.Exists(filePath))
                    {
                        Logger.Log(LogLevel.Warning, $"Content file not found: {contentFile.FilePath}");
                        continue;
                    }

                    var fileInfo = new FileInfo(filePath);
                    var extension = Path.GetExtension(filePath).ToLowerInvariant();
                    
                    // Handle DLL files as BepInEx plugins (no target needed)
                    if (extension == ".dll")
                    {
                        var pluginFile = new ModFile
                        {
                            Name = Path.GetFileNameWithoutExtension(filePath),
                            FilePath = filePath,
                            Type = ModType.BepInExPlugin,
                            FileSize = fileInfo.Length,
                            CreatedDate = fileInfo.CreationTime,
                            ModifiedDate = fileInfo.LastWriteTime
                        };
                        modFiles.Add(pluginFile);
                        continue;
                    }
                    
                    // For asset files, Target is required
                    if (string.IsNullOrWhiteSpace(contentFile.Target))
                    {
                        Logger.Log(LogLevel.Warning, $"ContentFile '{contentFile.FilePath}' missing Target - skipping");
                        continue;
                    }
                    
                    // Determine type from manifest or auto-detect
                    ModType modType;
                    if (!string.IsNullOrWhiteSpace(contentFile.Type))
                    {
                        var resolvedType = contentFile.Type.ToLowerInvariant() switch
                        {
                            "audio" => (ModType?)ModType.Audio,
                            "sprite" => (ModType?)ModType.Sprite,
                            "texture" => (ModType?)ModType.Texture,
                            "bepinexplugin" => (ModType?)ModType.BepInExPlugin,
                            _ => DetermineTypeFromExtension(extension)
                        };
                        if (resolvedType == null)
                        {
                            Logger.Log(LogLevel.Warning, $"Skipping '{contentFile.FilePath}': unsupported type '{contentFile.Type}'");
                            continue;
                        }
                        modType = resolvedType.Value;
                    }
                    else
                    {
                        var detectedType = DetermineTypeFromExtension(extension);
                        if (detectedType == null)
                        {
                            Logger.Log(LogLevel.Warning, $"Skipping '{contentFile.FilePath}': unsupported file extension '{extension}'");
                            continue;
                        }
                        modType = detectedType.Value;
                    }

                    var modFile = new ModFile
                    {
                        Name = contentFile.Target,
                        FilePath = filePath,
                        Type = modType,
                        FileSize = fileInfo.Length,
                        CreatedDate = fileInfo.CreationTime,
                        ModifiedDate = fileInfo.LastWriteTime
                    };

                    modFiles.Add(modFile);
                }
            }

            if (modFiles.Count == 0)
            {
                Logger.Log(LogLevel.Warning, $"Manifest-based mod has no valid files: {manifest.Name}");
                return null;
            }

            var folderMod = new FolderMod
            {
                Name = manifest.Name,
                FolderPath = folderPath,
                Description = manifest.Description ?? $"Manifest-based mod with {modFiles.Count} files",
                Version = manifest.Version,
                Author = manifest.Author,
                UniqueID = manifest.UniqueID,
                IsManifestBased = true,
                Manifest = manifest,
                CreatedDate = folderInfo.CreationTime,
                ModifiedDate = folderInfo.LastWriteTime
            };

            foreach (var modFile in modFiles)
            {
                folderMod.ModFiles.Add(modFile);
            }

            // Determine online status by inspecting each BepInEx plugin DLL for
            // the AffectsGameplayAttribute. A single gameplay-affecting DLL is
            // enough to mark the whole mod as Disabled.
            folderMod.OnlineStatus = ComputeOnlineStatus(folderMod);

            Logger.Log(LogLevel.Success, $"Loaded manifest-based mod '{manifest.Name}' with {modFiles.Count} files");
            return folderMod;
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, $"Error loading manifest-based mod from {folderPath}: {ex.Message}");
            return null;
        }
    }

    // -------------------------------------------------------------------------
    // Online status helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Inspects every BepInEx plugin DLL in <paramref name="mod"/> and returns:
    ///   NotApplicable — mod has no plugin DLLs (audio/texture-only)
    ///   Disabled      — at least one DLL has AffectsGameplay=true (or attribute absent)
    ///   Approved      — all DLLs explicitly carry AffectsGameplay=false
    /// </summary>
    private static ModOnlineStatus ComputeOnlineStatus(FolderMod mod)
    {
        var pluginFiles = mod.ModFiles
            .Where(f => f.Type == ModType.BepInExPlugin)
            .ToList();

        if (pluginFiles.Count == 0)
            return ModOnlineStatus.NotApplicable;

        foreach (var file in pluginFiles)
        {
            if (DllInspector.AffectsGameplay(file.FilePath))
                return ModOnlineStatus.Disabled;
        }

        return ModOnlineStatus.Approved;
    }

    private ModType? DetermineTypeFromExtension(string extension)
    {
        var audioExtensions = new[] { ".ogg", ".wav", ".mp3", ".m4a" };
        var imageExtensions = new[] { ".png", ".jpg", ".jpeg", ".bmp", ".tga" };

        if (audioExtensions.Contains(extension))
            return ModType.Audio;
        if (imageExtensions.Contains(extension))
            return ModType.Sprite;

        return null;
    }

    // -------------------------------------------------------------------------
    // Dependency helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns the loaded mods that are required dependencies of <paramref name="mod"/>.
    /// Only considers dependencies where <see cref="ModDependency.IsRequired"/> is true.
    /// Mods not found in <see cref="AvailableMods"/> are silently skipped (they will be
    /// caught by <see cref="ValidateDependencies"/> instead).
    /// </summary>
    public IEnumerable<FolderMod> GetDependencies(FolderMod mod)
    {
        var deps = mod.Manifest?.Dependencies;
        if (deps == null || deps.Count == 0)
            yield break;

        foreach (var dep in deps)
        {
            if (!dep.IsRequired) continue;
            var found = _availableMods.FirstOrDefault(m =>
                string.Equals(m.UniqueID, dep.UniqueID, StringComparison.OrdinalIgnoreCase));
            if (found != null)
                yield return found;
        }
    }

    /// <summary>
    /// Returns the currently-enabled loaded mods that declare <paramref name="mod"/>
    /// as a required dependency. Used to warn before disabling a mod.
    /// </summary>
    public IEnumerable<FolderMod> GetDependents(FolderMod mod)
    {
        foreach (var candidate in _availableMods)
        {
            if (!candidate.IsEnabled) continue;
            if (ReferenceEquals(candidate, mod)) continue;

            var deps = candidate.Manifest?.Dependencies;
            if (deps == null) continue;

            bool dependsOnMod = deps.Any(d =>
                d.IsRequired &&
                string.Equals(d.UniqueID, mod.UniqueID, StringComparison.OrdinalIgnoreCase));

            if (dependsOnMod)
                yield return candidate;
        }
    }

    /// <summary>
    /// Validates that every enabled mod has all its required dependencies enabled.
    /// Returns a list of human-readable error strings. An empty list means everything is OK.
    /// </summary>
    public List<string> ValidateDependencies()
    {
        var errors = new List<string>();

        foreach (var mod in _availableMods)
        {
            if (!mod.IsEnabled) continue;

            var deps = mod.Manifest?.Dependencies;
            if (deps == null || deps.Count == 0) continue;

            foreach (var dep in deps)
            {
                if (!dep.IsRequired) continue;

                var depMod = _availableMods.FirstOrDefault(m =>
                    string.Equals(m.UniqueID, dep.UniqueID, StringComparison.OrdinalIgnoreCase));

                if (depMod == null)
                {
                    errors.Add($"• \"{mod.Name}\" requires \"{dep.UniqueID}\" which is not installed.");
                }
                else if (!depMod.IsEnabled)
                {
                    errors.Add($"• \"{mod.Name}\" requires \"{depMod.Name}\" but it is disabled.");
                }
            }
        }

        return errors;
    }

}


