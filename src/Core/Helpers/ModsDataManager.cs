using WMO.Core.Logging;

namespace WMO.Core.Helpers;

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
    /// Scans the mods folder recursively and categorizes files into audio and sprite mods
    /// </summary>
    /// <returns>ModsCollection containing categorized audio and sprite mods</returns>
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

            // Get all files recursively from the mods directory
            var allFiles = Directory.GetFiles(ResourcesPath, "*.*", SearchOption.AllDirectories);
            Logger.Log(LogLevel.Debug, $"Found {allFiles.Length} total files in mods directory");

            var audioExtensions = new[] { ".ogg", ".wav", ".mp3", ".m4a" };
            var imageExtensions = new[] { ".png", ".jpg", ".jpeg", ".bmp", ".tga" };

            foreach (var filePath in allFiles)
            {
                var extension = Path.GetExtension(filePath).ToLowerInvariant();
                var fileName = Path.GetFileNameWithoutExtension(filePath);
                var relativePath = Path.GetRelativePath(ResourcesPath, filePath);

                if (audioExtensions.Contains(extension))
                {
                    var audioMod = ProcessAudioFile(filePath, fileName, relativePath);
                    if (audioMod != null)
                    {
                        collection.AudioMods.Add(audioMod);
                        Logger.Log(LogLevel.Debug, $"Added audio mod: {audioMod.AssetName} -> {audioMod.FilePath}");
                    }
                }
                else if (imageExtensions.Contains(extension))
                {
                    // Determine if this is a sprite or texture based on naming/path
                    bool isTexture = DetermineIfTexture(filePath, fileName, relativePath);
                    
                    if (isTexture)
                    {
                        var textureMod = ProcessTextureFile(filePath, fileName, relativePath);
                        if (textureMod != null)
                        {
                            collection.TextureMods.Add(textureMod);
                            Logger.Log(LogLevel.Debug, $"Added texture mod: {textureMod.AssetName} -> {textureMod.FilePath}");
                        }
                    }
                    else
                    {
                        var spriteMod = ProcessSpriteFile(filePath, fileName, relativePath);
                        if (spriteMod != null)
                        {
                            collection.SpriteMods.Add(spriteMod);
                            Logger.Log(LogLevel.Debug, $"Added sprite mod: {spriteMod.AssetName} -> {spriteMod.FilePath}");
                        }
                    }
                }
                else
                {
                    Logger.Log(LogLevel.Debug, $"Skipped unsupported file: {relativePath}");
                }
            }

            Logger.Log(LogLevel.Info, $"Mods collection prepared: {collection.AudioMods.Count} audio, {collection.SpriteMods.Count} sprites, {collection.TextureMods.Count} textures");
            
            if (collection.AudioMods.Count > 0)
            {
                Logger.Log(LogLevel.Info, $"Audio mods: {string.Join(", ", collection.AudioMods.Select(m => m.AssetName))}");
            }
            
            if (collection.SpriteMods.Count > 0)
            {
                Logger.Log(LogLevel.Info, $"Sprite mods: {string.Join(", ", collection.SpriteMods.Select(m => m.AssetName))}");
            }
            
            if (collection.TextureMods.Count > 0)
            {
                Logger.Log(LogLevel.Info, $"Texture mods: {string.Join(", ", collection.TextureMods.Select(m => m.AssetName))}");
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
    /// Processes an audio file and creates an AudioMod
    /// </summary>
    private static AudioMod? ProcessAudioFile(string filePath, string fileName, string relativePath)
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
            
            return new AudioMod
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
    /// Processes a sprite file and creates a SpriteMod
    /// </summary>
    private static SpriteMod? ProcessSpriteFile(string filePath, string fileName, string relativePath)
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
            
            return new SpriteMod
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
    /// Processes a texture file and creates a TextureMod
    /// </summary>
    private static TextureMod? ProcessTextureFile(string filePath, string fileName, string relativePath)
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
            
            return new TextureMod
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
    /// Gets the file path for a mod by its asset name, preferring OGG format for audio
    /// </summary>
    /// <param name="assetName">The name of the asset to replace</param>
    /// <returns>Full path to the mod file, or null if not found</returns>
    public static string? GetModFilePath(string assetName)
    {
        try
        {
            if (!Directory.Exists(ResourcesPath))
                return null;

            // Prioritize formats: OGG first for audio (best compatibility), then others
            var audioExtensions = new[] { ".ogg", ".wav", ".mp3", ".m4a" };
            var imageExtensions = new[] { ".png", ".jpg", ".jpeg", ".bmp", ".tga" };
            var allExtensions = audioExtensions.Concat(imageExtensions).ToArray();

            // Look for files that start with "RE" + assetName with any supported extension
            // Check in priority order (OGG first for audio)
            foreach (var extension in allExtensions)
            {

                var fullPath = Path.Combine(ResourcesPath, $"{assetName}{extension}");
                if (File.Exists(fullPath))
                {

                    Logger.Log(LogLevel.Debug, $"Selected mod file for '{assetName}': {Path.GetFileName(fullPath)}");
                    return fullPath;
                }
            }


            // Search in subdirectories as well
            var allModFiles = Directory.GetFiles(ResourcesPath, "*.*", SearchOption.AllDirectories)
                .Where(f => allExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()));
        
            // Group by processed asset name, then prioritize by format
            var matchingFiles = new List<string>();
            
            foreach (var file in allModFiles)
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                var processedName = fileName.StartsWith("RE", StringComparison.OrdinalIgnoreCase) 
                    ? fileName.Substring(2) 
                    : fileName.StartsWith("Re", StringComparison.OrdinalIgnoreCase)
                        ? fileName.Substring(2)
                        : fileName;

                if (string.Equals(processedName, assetName, StringComparison.OrdinalIgnoreCase))
                {
                    matchingFiles.Add(file);
                }
            }
            
            // If multiple files found, prefer OGG for audio
            if (matchingFiles.Count > 0)
            {
                // Sort by format preference: OGG first, then others
                
                var selectedFile = matchingFiles.First();
                var selectedExt = Path.GetExtension(selectedFile).ToLowerInvariant();
                
                if (matchingFiles.Count > 1)
                {
                    Logger.Log(LogLevel.Info, $"Multiple mod files found for '{assetName}', selected {selectedExt.ToUpper()} format: {Path.GetFileName(selectedFile)}");
                }
                
                
                Logger.Log(LogLevel.Debug, $"Selected mod file for '{assetName}': {Path.GetFileName(selectedFile)}");
                return selectedFile;
            }

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

Place your mod files in this folder to replace game assets.

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

FINDING ASSET NAMES:
Run the patcher at least once to generate 'assetListNames.json' in this folder.
This JSON file contains ALL asset names found in the game, organized by type.
Use this file to find the exact names of assets you want to replace.

EXAMPLE MOD FILES:
- bgm-lobby.ogg (replaces background music)
- bgm-title.ogg (replaces title screen music)
- sfxUI-cancel.ogg (replaces UI cancel sound)
- head-default-0.png (replaces character head sprite)
- body-default-0.png (replaces character body sprite)
After placing your mod files here, run the patcher to apply them to the game.
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
/// Collection of all mods categorized by type
/// </summary>
public class ModsCollection
{
    public List<AudioMod> AudioMods { get; set; } = new();
    public List<SpriteMod> SpriteMods { get; set; } = new();
    public List<TextureMod> TextureMods { get; set; } = new();
    
    public int TotalCount => AudioMods.Count + SpriteMods.Count + TextureMods.Count;
}

/// <summary>
/// Base class for all mod types
/// </summary>
public abstract class ModBase
{
    public string AssetName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;

    public string FileExtension => Path.GetExtension(FilePath).ToLowerInvariant();
}

/// <summary>
/// Represents an audio mod file
/// </summary>
public class AudioMod : ModBase
{
    public override string ToString() => $"Audio: {AssetName} ({Path.GetFileName(FilePath)}, {FileExtension})";
}

/// <summary>
/// Represents a sprite/texture mod file
/// </summary>
public class SpriteMod : ModBase
{
    public override string ToString() => $"Sprite: {AssetName} ({Path.GetFileName(FilePath)})";
}

/// <summary>
/// Represents a texture mod file (Texture2D assets)
/// </summary>
public class TextureMod : ModBase
{
    public override string ToString() => $"Texture: {AssetName} ({Path.GetFileName(FilePath)})";
}
