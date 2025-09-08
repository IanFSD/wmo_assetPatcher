using System.Collections.ObjectModel;
using WMO.Core.Models;
using WMO.Core.Models.Enums;
using WMO.Core.Logging;
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
    /// </summary>
    private FolderMod? ScanModFolder(string folderPath)
    {
        var folderInfo = new DirectoryInfo(folderPath);
        var folderName = folderInfo.Name;
        
        Logger.Log(LogLevel.Debug, $"Scanning mod folder: {folderName}");
        
        // Get all files in the folder and subfolders
        var files = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories);
        var audioExtensions = new[] { ".ogg", ".wav", ".mp3", ".m4a" };
        var imageExtensions = new[] { ".png", ".jpg", ".jpeg", ".bmp", ".tga" };
        
        var modFiles = new List<ModFile>();
        
        foreach (var filePath in files)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var fileInfo = new FileInfo(filePath);
            
            ModType? modType = null;
            
            if (audioExtensions.Contains(extension))
            {
                modType = ModType.Audio;
            }
            else if (imageExtensions.Contains(extension))
            {
                // Determine if sprite or texture based on path/name
                modType = DetermineImageType(filePath, fileName);
            }
            else if (extension == ".json" && fileName.ToLowerInvariant() == "mod")
            {
                // Skip mod.json files - they contain metadata
                continue;
            }
            
            if (modType.HasValue)
            {
                var modFile = new ModFile
                {
                    Name = fileName,
                    FilePath = filePath,
                    Type = modType.Value,
                    FileSize = fileInfo.Length,
                    CreatedDate = fileInfo.CreationTime,
                    ModifiedDate = fileInfo.LastWriteTime
                };
                
                modFiles.Add(modFile);
            }
        }
        
        // Skip folders with no valid mod files
        if (modFiles.Count == 0)
        {
            Logger.Log(LogLevel.Debug, $"Skipping folder {folderName} - no valid mod files found");
            return null;
        }
        
        // Try to load mod metadata from mod.json
        var modMetadata = LoadModMetadata(folderPath);
        
        var folderMod = new FolderMod
        {
            Name = modMetadata?.Name ?? folderName,
            FolderPath = folderPath,
            Description = modMetadata?.Description ?? $"Mod with {modFiles.Count} files ({modFiles.Count(f => f.Type == ModType.Audio)} audio, {modFiles.Count(f => f.Type == ModType.Sprite)} sprite, {modFiles.Count(f => f.Type == ModType.Texture)} texture)",
            Version = modMetadata?.Version,
            Author = modMetadata?.Author,
            CreatedDate = folderInfo.CreationTime,
            ModifiedDate = folderInfo.LastWriteTime
        };
        
        // Add all mod files to the folder mod
        foreach (var modFile in modFiles)
        {
            folderMod.ModFiles.Add(modFile);
        }
        
        Logger.Log(LogLevel.Debug, $"Loaded mod '{folderMod.Name}' with {modFiles.Count} files");
        return folderMod;
    }
    
    /// <summary>
    /// Loads mod metadata from mod.json file if it exists
    /// </summary>
    private ModMetadata? LoadModMetadata(string folderPath)
    {
        var metadataPath = Path.Combine(folderPath, "mod.json");
        
        if (!File.Exists(metadataPath))
        {
            return null;
        }
        
        try
        {
            var json = File.ReadAllText(metadataPath);
            return JsonSerializer.Deserialize<ModMetadata>(json, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Warning, $"Failed to parse mod.json in {folderPath}: {ex.Message}");
            return null;
        }
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
                var metadata = new ModMetadata
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
    
    private ModType DetermineImageType(string filePath, string fileName)
    {
        // Simple heuristic: if the path contains "sprite" or filename suggests sprite, it's a sprite
        var lowerPath = filePath.ToLowerInvariant();
        var lowerName = fileName.ToLowerInvariant();
        
        if (lowerPath.Contains("sprite") || lowerName.Contains("sprite") || 
            lowerName.Contains("icon") || lowerName.Contains("ui"))
        {
            return ModType.Sprite;
        }
        
        return ModType.Texture;
    }
}


