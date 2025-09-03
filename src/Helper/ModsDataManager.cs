using WMO.Logging;

namespace WMO.Helper;

public static class ModsDataManager
{
    private static ModsCollection? _modsCollection;
    private static readonly string ResourcesPath = GetResourcesPath();
    
    /// <summary>
    /// Gets the complete mods collection with categorized audio and sprite mods
    /// </summary>
    public static ModsCollection GetModsCollection()
    {
        _modsCollection ??= PrepareModsCollection();
        return _modsCollection;
    }

    /// <summary>
    /// Scans the mods folder for mod packages (folders) and categorizes their contents
    /// </summary>
    /// <returns>ModsCollection containing mod packages with categorized files</returns>
    private static ModsCollection PrepareModsCollection()
    {
        var collection = new ModsCollection();
        
        try
        {
            Logger.Log(LogLevel.Info, $"Preparing mods collection from resources...");

            if (!Directory.Exists(ResourcesPath))
            {
                Logger.Log(LogLevel.Warning, $"Resources directory not found at: {ResourcesPath}");
                return collection;
            }

            // Get all subdirectories (mod packages) in the mods directory
            var modDirectories = Directory.GetDirectories(ResourcesPath);
            Logger.Log(LogLevel.Debug, $"Found {modDirectories.Length} mod directories in mods folder");

            // Also check for loose files in the root for backward compatibility
            var looseFiles = Directory.GetFiles(ResourcesPath, "*.*", SearchOption.TopDirectoryOnly);
            if (looseFiles.Length > 0)
            {
                Logger.Log(LogLevel.Info, $"Found {looseFiles.Length} loose files in mods root - creating compatibility mod package");
                var compatibilityMod = ProcessModDirectory(ResourcesPath, "Legacy Files", true);
                if (compatibilityMod != null && compatibilityMod.TotalAssetCount > 0)
                {
                    collection.ModPackages.Add(compatibilityMod);
                }
            }

            // Process each mod directory
            foreach (var modDirectory in modDirectories)
            {
                var modName = Path.GetFileName(modDirectory);
                Logger.Log(LogLevel.Debug, $"Processing mod directory: {modName}");
                
                var modPackage = ProcessModDirectory(modDirectory, modName, false);
                if (modPackage != null && modPackage.TotalAssetCount > 0)
                {
                    collection.ModPackages.Add(modPackage);
                    Logger.Log(LogLevel.Info, $"Added mod package '{modName}' with {modPackage.TotalAssetCount} assets");
                }
                else
                {
                    Logger.Log(LogLevel.Warning, $"Mod directory '{modName}' contains no valid assets");
                }
            }

            Logger.Log(LogLevel.Info, $"Mods collection prepared: {collection.ModPackages.Count} mod packages with {collection.TotalAssetCount} total assets");
            
            foreach (var modPackage in collection.ModPackages)
            {
                Logger.Log(LogLevel.Info, $"Mod Package '{modPackage.Name}': " +
                    $"{modPackage.AudioAssets.Count} audio, " +
                    $"{modPackage.SpriteAssets.Count} sprites, " +
                    $"{modPackage.TextureAssets.Count} textures, " +
                    $"{modPackage.MonoBehaviourAssets.Count} monobehaviours");
            }

            return collection;
        }
        catch (Exception ex)
        {
            ErrorHandler.Handle("Failed to prepare mods collection", ex);
            return collection;
        }
    }

    /// <summary>
    /// Processes a mod directory and creates a ModPackage with all its assets
    /// </summary>
    /// <param name="directoryPath">Path to the mod directory</param>
    /// <param name="modName">Name of the mod</param>
    /// <param name="isRootLevel">Whether this is processing the root directory for loose files</param>
    /// <returns>ModPackage containing all assets found in the directory</returns>
    private static ModPackage? ProcessModDirectory(string directoryPath, string modName, bool isRootLevel)
    {
        try
        {
            var modPackage = new ModPackage { Name = modName, DirectoryPath = directoryPath };
            
            // Get all files in the directory (recursive for mod directories, non-recursive for root level loose files)
            var searchOption = isRootLevel ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories;
            var allFiles = Directory.GetFiles(directoryPath, "*.*", searchOption);
            
            var audioExtensions = new[] { ".ogg", ".wav", ".mp3", ".m4a" };
            var imageExtensions = new[] { ".png", ".jpg", ".jpeg", ".bmp", ".tga" };
            var monoBehaviourExtensions = new[] { ".json", ".bytes" };

            Logger.Log(LogLevel.Debug, $"Processing {allFiles.Length} files in mod '{modName}'");

            foreach (var filePath in allFiles)
            {
                var extension = Path.GetExtension(filePath).ToLowerInvariant();
                var fileName = Path.GetFileNameWithoutExtension(filePath);
                var relativePath = Path.GetRelativePath(directoryPath, filePath);

                // Skip hidden files and directories
                if (fileName.StartsWith('.') || relativePath.Contains("\\.") || relativePath.Contains("/."))
                {
                    Logger.Log(LogLevel.Debug, $"Skipped hidden file: {relativePath}");
                    continue;
                }

                if (audioExtensions.Contains(extension))
                {
                    var audioAsset = ProcessAudioFile(filePath, fileName, relativePath);
                    if (audioAsset != null)
                    {
                        modPackage.AudioAssets.Add(audioAsset);
                        Logger.Log(LogLevel.Debug, $"Added audio asset to '{modName}': {audioAsset.AssetName}");
                    }
                }
                else if (imageExtensions.Contains(extension))
                {
                    // Determine if this is a sprite or texture based on naming/path
                    bool isTexture = DetermineIfTexture(filePath, fileName, relativePath);
                    
                    if (isTexture)
                    {
                        var textureAsset = ProcessTextureFile(filePath, fileName, relativePath);
                        if (textureAsset != null)
                        {
                            modPackage.TextureAssets.Add(textureAsset);
                            Logger.Log(LogLevel.Debug, $"Added texture asset to '{modName}': {textureAsset.AssetName}");
                        }
                    }
                    else
                    {
                        var spriteAsset = ProcessSpriteFile(filePath, fileName, relativePath);
                        if (spriteAsset != null)
                        {
                            modPackage.SpriteAssets.Add(spriteAsset);
                            Logger.Log(LogLevel.Debug, $"Added sprite asset to '{modName}': {spriteAsset.AssetName}");
                        }
                    }
                }
                else if (monoBehaviourExtensions.Contains(extension))
                {
                    var monoBehaviourAsset = ProcessMonoBehaviourFile(filePath, fileName, relativePath);
                    if (monoBehaviourAsset != null)
                    {
                        modPackage.MonoBehaviourAssets.Add(monoBehaviourAsset);
                        Logger.Log(LogLevel.Debug, $"Added MonoBehaviour asset to '{modName}': {monoBehaviourAsset.AssetName}");
                    }
                }
                else
                {
                    Logger.Log(LogLevel.Debug, $"Skipped unsupported file in '{modName}': {relativePath}");
                }
            }

            return modPackage;
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, $"Failed to process mod directory '{modName}': {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Processes a MonoBehaviour file and creates a MonoBehaviourAsset
    /// </summary>
    private static MonoBehaviourAsset? ProcessMonoBehaviourFile(string filePath, string fileName, string relativePath)
    {
        try
        {
            // Process the filename to extract the asset name
            var assetName = ProcessModFileName(fileName);
            if (string.IsNullOrEmpty(assetName))
            {
                Logger.Log(LogLevel.Warning, $"Could not determine asset name from file: {fileName}");
                return null;
            }

            Logger.Log(LogLevel.Debug, $"Processed '{fileName}' -> MonoBehaviour asset name: '{assetName}'");
            
            return new MonoBehaviourAsset
            {
                AssetName = assetName,
                FilePath = filePath,
                RelativePath = relativePath,
                OriginalFileName = fileName
            };
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Warning, $"Failed to process MonoBehaviour file {relativePath}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Processes an audio file and creates an AudioAsset
    /// </summary>
    private static AudioAsset? ProcessAudioFile(string filePath, string fileName, string relativePath)
    {
        try
        {
            // Process the filename to extract the asset name
            var assetName = ProcessModFileName(fileName);
            if (string.IsNullOrEmpty(assetName))
            {
                Logger.Log(LogLevel.Warning, $"Could not determine asset name from file: {fileName}");
                return null;
            }

            Logger.Log(LogLevel.Debug, $"Processed '{fileName}' -> asset name: '{assetName}'");
            
            return new AudioAsset
            {
                AssetName = assetName,
                FilePath = filePath,
                RelativePath = relativePath,
                OriginalFileName = fileName
            };
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Warning, $"Failed to process audio file {relativePath}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Processes a sprite file and creates a SpriteAsset
    /// </summary>
    private static SpriteAsset? ProcessSpriteFile(string filePath, string fileName, string relativePath)
    {
        try
        {
            // Process the filename to extract the asset name
            var assetName = ProcessModFileName(fileName);
            if (string.IsNullOrEmpty(assetName))
            {
                Logger.Log(LogLevel.Warning, $"Could not determine asset name from file: {fileName}");
                return null;
            }

            Logger.Log(LogLevel.Debug, $"Processed '{fileName}' -> asset name: '{assetName}'");
            
            return new SpriteAsset
            {
                AssetName = assetName,
                FilePath = filePath,
                RelativePath = relativePath,
                OriginalFileName = fileName
            };
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Warning, $"Failed to process sprite file {relativePath}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Processes a texture file and creates a TextureAsset
    /// </summary>
    private static TextureAsset? ProcessTextureFile(string filePath, string fileName, string relativePath)
    {
        try
        {
            // Process the filename to extract the asset name
            var assetName = ProcessModFileName(fileName);
            if (string.IsNullOrEmpty(assetName))
            {
                Logger.Log(LogLevel.Warning, $"Could not determine asset name from file: {fileName}");
                return null;
            }

            Logger.Log(LogLevel.Debug, $"Processed '{fileName}' -> texture asset name: '{assetName}'");
            
            return new TextureAsset
            {
                AssetName = assetName,
                FilePath = filePath,
                RelativePath = relativePath,
                OriginalFileName = fileName
            };
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Warning, $"Failed to process texture file {relativePath}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Determines if an image file should be treated as a texture (Texture2D) or sprite based on naming conventions
    /// </summary>
    /// <param name="filePath">Full path to the file</param>
    /// <param name="fileName">File name without extension</param>
    /// <param name="relativePath">Relative path from mods folder</param>
    /// <returns>True if should be treated as texture, false if sprite</returns>
    private static bool DetermineIfTexture(string filePath, string fileName, string relativePath)
    {
        // Convert to lowercase for case-insensitive matching
        var lowerFileName = fileName.ToLowerInvariant();
        var lowerRelativePath = relativePath.ToLowerInvariant();
        
        // Check for texture-specific keywords in the filename or path
        var textureKeywords = new[] { "texture", "tex", "material", "mat", "diffuse", "normal", "bump", "specular", "roughness", "metallic", "albedo", "basecolor" };
        var spriteKeywords = new[] { "sprite", "icon", "ui", "button", "logo", "avatar", "character", "portrait" };
        
        // Check if in a textures folder
        if (lowerRelativePath.Contains("texture") || lowerRelativePath.Contains("material"))
        {
            Logger.Log(LogLevel.Debug, $"File '{fileName}' classified as texture due to path: {relativePath}");
            return true;
        }
        
        // Check if in a sprites folder  
        if (lowerRelativePath.Contains("sprite") || lowerRelativePath.Contains("ui") || lowerRelativePath.Contains("icon"))
        {
            Logger.Log(LogLevel.Debug, $"File '{fileName}' classified as sprite due to path: {relativePath}");
            return false;
        }
        
        // Check filename for texture keywords
        foreach (var keyword in textureKeywords)
        {
            if (lowerFileName.Contains(keyword))
            {
                Logger.Log(LogLevel.Debug, $"File '{fileName}' classified as texture due to keyword: {keyword}");
                return true;
            }
        }
        
        // Check filename for sprite keywords
        foreach (var keyword in spriteKeywords)
        {
            if (lowerFileName.Contains(keyword))
            {
                Logger.Log(LogLevel.Debug, $"File '{fileName}' classified as sprite due to keyword: {keyword}");
                return false;
            }
        }
        
        // Default classification: if no specific indicators, treat as sprite
        // This maintains compatibility with existing sprite mods
        Logger.Log(LogLevel.Debug, $"File '{fileName}' classified as sprite (default)");
        return false;
    }

    /// <summary>
    /// Gets the file path for a mod by its asset name from all mod packages
    /// </summary>
    /// <param name="assetName">The name of the asset to replace</param>
    /// <returns>Full path to the mod file, or null if not found</returns>
    public static string? GetModFilePath(string assetName)
    {
        try
        {
            var modsCollection = GetModsCollection();
            
            // Search through all assets in all mod packages
            var allAssets = modsCollection.GetAllAudioAssets()
                .Cast<AssetBase>()
                .Concat(modsCollection.GetAllSpriteAssets())
                .Concat(modsCollection.GetAllTextureAssets())
                .Concat(modsCollection.GetAllMonoBehaviourAssets());

            // Find matching asset by name (case-insensitive)
            var matchingAsset = allAssets.FirstOrDefault(asset => 
                string.Equals(asset.AssetName, assetName, StringComparison.OrdinalIgnoreCase));

            if (matchingAsset != null && File.Exists(matchingAsset.FilePath))
            {
                Logger.Log(LogLevel.Debug, $"Found mod file for '{assetName}': {Path.GetFileName(matchingAsset.FilePath)}");
                return matchingAsset.FilePath;
            }

            Logger.Log(LogLevel.Debug, $"No mod file found for asset: {assetName}");
            return null;
        }
        catch (Exception ex)
        {
            ErrorHandler.Handle($"Failed to get mod file path for asset '{assetName}'", ex);
            return null;
        }
    }

    /// <summary>
    /// Processes a mod filename to extract the actual asset name
    /// Handles various naming conventions like 'RE', 'Re', or plain filename
    /// </summary>
    /// <param name="fileName">Original filename without extension</param>
    /// <returns>Asset name to search for in game files</returns>
    private static string ProcessModFileName(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return string.Empty;
            
        // Check for various prefixes and remove them to get the actual asset name
        if (fileName.StartsWith("RE", StringComparison.OrdinalIgnoreCase))
        {
            // Remove 'RE' or 'Re' prefix
            return fileName.Substring(2);
        }
        
        // If no prefix, use the filename as-is
        return fileName;
    }
    
    /// <summary>
    /// Forces a refresh of the mods data by rescanning the resources directory
    /// </summary>
    public static void RefreshModsData()
    {
        _modsCollection = null;
        Logger.Log(LogLevel.Info, $"Mods data cache cleared, will be refreshed on next access");
    }

    /// <summary>
    /// Gets the path to the mods directory next to the executable
    /// </summary>
    /// <returns>Path to the mods directory</returns>
    private static string GetResourcesPath()
    {
        // Get the directory where the executable is located
        var exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var modsPath = Path.Combine(exeDirectory, "mods");
        
        // Create the mods directory if it doesn't exist
        try
        {
            if (!Directory.Exists(modsPath))
            {
                Directory.CreateDirectory(modsPath);
                Logger.Log(LogLevel.Info, $"Created mods directory at: {modsPath}");
                
                // Create a README file to explain how to use the mods folder
                var readmePath = Path.Combine(modsPath, "README.txt");
                var readmeContent = @"WMO Asset Patcher - Mods Folder
================================

MOD PACKAGE SYSTEM:
Each subfolder in this directory represents a complete mod package.
You can still place individual files in the root for backward compatibility.

FOLDER STRUCTURE:
mods/
├── MyAwesomeMod/          (Mod package folder)
│   ├── bgm-lobby.ogg      (Audio file)
│   ├── head-default-0.png (Sprite file)
│   └── config.json        (MonoBehaviour file)
├── AnotherMod/            (Another mod package)
│   ├── sfxUI-cancel.ogg
│   └── some-texture.png
└── legacy-file.ogg        (Legacy compatibility - single file)

FILE NAMING CONVENTION:
- Your mod files should be named exactly like the asset name you want to replace.
- Example: bgm-lobby.ogg will replace the 'bgm-lobby' asset in the game
- Example: head-default-0.png will replace the 'head-default-0' sprite

SUPPORTED FORMATS:
Audio:
- .ogg (recommended)
- .wav
- .mp3 (not recommended)

Sprites/Textures:
- .png (recommended)
- .jpg
- .jpeg

MonoBehaviours:
- .json (field-level modifications)
- .bytes (complete replacement)

FINDING ASSET NAMES:
Run the patcher with --debug flag to scan and list all MonoBehaviours and other assets.
Use this information to find the exact names of assets you want to replace.

EXAMPLE MOD PACKAGE:
Create a folder called 'MyMod' and place your files inside:
MyMod/
├── bgm-lobby.ogg          (replaces background music)
├── bgm-title.ogg          (replaces title screen music)
├── sfxUI-cancel.ogg       (replaces UI cancel sound)
├── head-default-0.png     (replaces character head sprite)
├── body-default-0.png     (replaces character body sprite)
└── PlayerController.json  (modifies MonoBehaviour fields)

After creating your mod packages, run the patcher to apply them to the game.
";
                File.WriteAllText(readmePath, readmeContent);
                Logger.Log(LogLevel.Info, $"Created README.txt in mods folder");
            }
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, $"Failed to create mods directory: {ex.Message}");
        }
        
        Logger.Log(LogLevel.Debug, $"Using mods directory: {modsPath}");
        return modsPath;
    }
}

/// <summary>
/// Collection of all mod packages
/// </summary>
public class ModsCollection
{
    public List<ModPackage> ModPackages { get; set; } = new();
    
    public int TotalAssetCount => ModPackages.Sum(mp => mp.TotalAssetCount);
    
    /// <summary>
    /// Gets all audio assets from all mod packages
    /// </summary>
    public IEnumerable<AudioAsset> GetAllAudioAssets()
    {
        return ModPackages.SelectMany(mp => mp.AudioAssets);
    }
    
    /// <summary>
    /// Gets all sprite assets from all mod packages
    /// </summary>
    public IEnumerable<SpriteAsset> GetAllSpriteAssets()
    {
        return ModPackages.SelectMany(mp => mp.SpriteAssets);
    }
    
    /// <summary>
    /// Gets all texture assets from all mod packages
    /// </summary>
    public IEnumerable<TextureAsset> GetAllTextureAssets()
    {
        return ModPackages.SelectMany(mp => mp.TextureAssets);
    }
    
    /// <summary>
    /// Gets all MonoBehaviour assets from all mod packages
    /// </summary>
    public IEnumerable<MonoBehaviourAsset> GetAllMonoBehaviourAssets()
    {
        return ModPackages.SelectMany(mp => mp.MonoBehaviourAssets);
    }
}

/// <summary>
/// Represents a complete mod package containing multiple asset files
/// </summary>
public class ModPackage
{
    public string Name { get; set; } = string.Empty;
    public string DirectoryPath { get; set; } = string.Empty;
    public List<AudioAsset> AudioAssets { get; set; } = new();
    public List<SpriteAsset> SpriteAssets { get; set; } = new();
    public List<TextureAsset> TextureAssets { get; set; } = new();
    public List<MonoBehaviourAsset> MonoBehaviourAssets { get; set; } = new();
    
    public int TotalAssetCount => AudioAssets.Count + SpriteAssets.Count + TextureAssets.Count + MonoBehaviourAssets.Count;
    
    /// <summary>
    /// Gets all audio assets
    /// </summary>
    public IEnumerable<AudioAsset> GetAudioAssets() => AudioAssets;
    
    /// <summary>
    /// Gets all sprite assets
    /// </summary>
    public IEnumerable<SpriteAsset> GetSpriteAssets() => SpriteAssets;
    
    /// <summary>
    /// Gets all texture assets
    /// </summary>
    public IEnumerable<TextureAsset> GetTextureAssets() => TextureAssets;
    
    /// <summary>
    /// Gets all MonoBehaviour assets
    /// </summary>
    public IEnumerable<MonoBehaviourAsset> GetMonoBehaviourAssets() => MonoBehaviourAssets;
    
    public override string ToString() => $"ModPackage '{Name}': {TotalAssetCount} assets " +
        $"(Audio: {AudioAssets.Count}, Sprites: {SpriteAssets.Count}, Textures: {TextureAssets.Count}, MonoBehaviours: {MonoBehaviourAssets.Count})";
}

/// <summary>
/// Base class for all asset types
/// </summary>
public abstract class AssetBase
{
    public string AssetName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;

    public string FileExtension => Path.GetExtension(FilePath).ToLowerInvariant();
}

/// <summary>
/// Represents an audio asset file
/// </summary>
public class AudioAsset : AssetBase
{
    public override string ToString() => $"Audio: {AssetName} ({Path.GetFileName(FilePath)}, {FileExtension})";
}

/// <summary>
/// Represents a sprite asset file
/// </summary>
public class SpriteAsset : AssetBase
{
    public override string ToString() => $"Sprite: {AssetName} ({Path.GetFileName(FilePath)})";
}

/// <summary>
/// Represents a texture asset file (Texture2D assets)
/// </summary>
public class TextureAsset : AssetBase
{
    public override string ToString() => $"Texture: {AssetName} ({Path.GetFileName(FilePath)})";
}

/// <summary>
/// Represents a MonoBehaviour asset file (JSON or bytes)
/// </summary>
public class MonoBehaviourAsset : AssetBase
{
    public override string ToString() => $"MonoBehaviour: {AssetName} ({Path.GetFileName(FilePath)}, {FileExtension})";
}
