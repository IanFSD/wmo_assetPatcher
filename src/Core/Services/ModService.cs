using System.Collections.ObjectModel;
using WMO.Core.Models;
using WMO.Core.Helpers;
using WMO.Core.Logging;

namespace WMO.Core.Services;

/// <summary>
/// Service for managing mods discovery, installation, and management
/// </summary>
public class ModService
{
    private readonly ObservableCollection<ModInfo> _availableMods = new();
    private readonly string _modsDirectory;
    
    public ModService()
    {
        _modsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mods");
    }
    
    /// <summary>
    /// Collection of available mods
    /// </summary>
    public ObservableCollection<ModInfo> AvailableMods => _availableMods;
    
    /// <summary>
    /// Scans for and loads all available mods
    /// </summary>
    public async Task RefreshModsAsync()
    {
        await Task.Run(() =>
        {
            _availableMods.Clear();
            
            if (!Directory.Exists(_modsDirectory))
            {
                Logger.Log(LogLevel.Info, $"Mods directory not found: {_modsDirectory}");
                return;
            }
            
            var files = Directory.GetFiles(_modsDirectory, "*.*", SearchOption.AllDirectories);
            var audioExtensions = new[] { ".ogg", ".wav", ".mp3", ".m4a" };
            var imageExtensions = new[] { ".png", ".jpg", ".jpeg" };

            foreach (var filePath in files)
            {
                var extension = Path.GetExtension(filePath).ToLowerInvariant();
                var fileName = Path.GetFileNameWithoutExtension(filePath);
                var fileInfo = new FileInfo(filePath);
                
                ModType modType;
                if (audioExtensions.Contains(extension))
                {
                    modType = ModType.Audio;
                }
                else if (imageExtensions.Contains(extension))
                {
                    // Determine if sprite or texture based on path/name
                    modType = DetermineImageType(filePath, fileName);
                }
                else
                {
                    continue; // Skip unsupported files
                }
                
                var modInfo = new ModInfo
                {
                    Name = fileName,
                    FilePath = filePath,
                    Type = modType,
                    FileSize = fileInfo.Length,
                    CreatedDate = fileInfo.CreationTime,
                    ModifiedDate = fileInfo.LastWriteTime,
                    Description = GenerateDescription(fileName, modType),
                    IsEnabled = true
                };
                
                // Add to collection (we're not using WPF yet, so just add directly)
                _availableMods.Add(modInfo);
            }
            
            Logger.Log(LogLevel.Info, $"Loaded {_availableMods.Count} mods from {_modsDirectory}");
        });
    }
    
    /// <summary>
    /// Installs a mod from a file path
    /// </summary>
    public async Task<bool> InstallModAsync(string sourceFilePath)
    {
        try
        {
            if (!Directory.Exists(_modsDirectory))
            {
                Directory.CreateDirectory(_modsDirectory);
            }
            
            var fileName = Path.GetFileName(sourceFilePath);
            var targetPath = Path.Combine(_modsDirectory, fileName);
            
            // Check if file already exists
            if (File.Exists(targetPath))
            {
                Logger.Log(LogLevel.Warning, $"Mod file already exists: {fileName}");
                return false;
            }
            
            await Task.Run(() => File.Copy(sourceFilePath, targetPath));
            Logger.Log(LogLevel.Success, $"Installed mod: {fileName}");
            
            // Refresh the mods list
            await RefreshModsAsync();
            return true;
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, $"Failed to install mod from {sourceFilePath}: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Removes a mod from the collection and file system
    /// </summary>
    public async Task<bool> RemoveModAsync(ModInfo modInfo)
    {
        try
        {
            await Task.Run(() =>
            {
                if (File.Exists(modInfo.FilePath))
                {
                    File.Delete(modInfo.FilePath);
                }
            });
            
            _availableMods.Remove(modInfo);
            Logger.Log(LogLevel.Success, $"Removed mod: {modInfo.Name}");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, $"Failed to remove mod {modInfo.Name}: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Gets the legacy ModsCollection for compatibility with existing patching code
    /// </summary>
    public ModsCollection GetLegacyModsCollection()
    {
        var legacyCollection = ModsDataManager.GetModsCollection();
        
        // Update enabled status based on UI selections
        foreach (var mod in _availableMods.Where(m => !m.IsEnabled))
        {
            // Remove disabled mods from legacy collection
            // This would require modifications to ModsCollection to support removal
            // For now, we'll work with the existing system
        }
        
        return legacyCollection;
    }
    
    /// <summary>
    /// Opens the mods directory in Windows Explorer
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
    
    private string GenerateDescription(string fileName, ModType type)
    {
        return type switch
        {
            ModType.Audio => $"Audio replacement for {fileName}",
            ModType.Sprite => $"Sprite replacement for {fileName}",
            ModType.Texture => $"Texture replacement for {fileName}",
            _ => $"Mod file: {fileName}"
        };
    }
}
