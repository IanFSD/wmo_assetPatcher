using System;
using System.IO;
using WMO.Logging;

namespace WMO.AssetPatcher;

public static class AssetFileFinder
{
    /// <summary>
    /// Finds all .assets files in the specified directory and its subdirectories
    /// </summary>
    /// <param name="directoryPath">Root directory to search in</param>
    /// <param name="recursive">Whether to search recursively in subdirectories</param>
    /// <returns>Array of paths to .assets files</returns>
    public static string[] FindAssetsFiles(string directoryPath, bool recursive = true)
    {
        if (!Directory.Exists(directoryPath))
            throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");

        Logger.Log(LogLevel.Debug, $"Searching for .assets files in: {directoryPath} (recursive: {recursive})");
        
        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var files = Directory.GetFiles(directoryPath, "*.assets", searchOption);
        
        if (files.Length == 0)
            throw new FileNotFoundException("No .assets file found in the directory.");
            
        Logger.Log(LogLevel.Info, $"Found {files.Length} .assets files");
        return files;
    }

    /// <summary>
    /// Finds the first .assets file in the specified directory
    /// </summary>
    /// <param name="directoryPath">Directory to search in</param>
    /// <param name="recursive">Whether to search recursively in subdirectories</param>
    /// <returns>Path to the first .assets file found</returns>
    public static string FindFirstAssetFile(string directoryPath, bool recursive = true)
    {
        if (!Directory.Exists(directoryPath))
            throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");

        Logger.Log(LogLevel.Debug, $"Searching for first .assets file in: {directoryPath} (recursive: {recursive})");
        
        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var files = Directory.GetFiles(directoryPath, "*.assets", searchOption);
        
        if (files.Length == 0)
            throw new FileNotFoundException("No .assets file found in the directory.");

        Logger.Log(LogLevel.Debug, $"Found first .assets file: {files[0]}");
        return files[0];
    }

    /// <summary>
    /// Finds specific .assets files by name pattern
    /// </summary>
    /// <param name="directoryPath">Directory to search in</param>
    /// <param name="namePattern">Pattern to match (supports wildcards)</param>
    /// <param name="recursive">Whether to search recursively</param>
    /// <returns>Array of matching .assets file paths</returns>
    public static string[] FindAssetsByPattern(string directoryPath, string namePattern, bool recursive = true)
    {
        if (!Directory.Exists(directoryPath))
            throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");

        Logger.Log(LogLevel.Debug, $"Searching for .assets files matching pattern '{namePattern}' in: {directoryPath}");
        
        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var files = Directory.GetFiles(directoryPath, $"{namePattern}.assets", searchOption);
        
        Logger.Log(LogLevel.Debug, $"Found {files.Length} .assets files matching pattern '{namePattern}'");
        return files;
    }

    /// <summary>
    /// Gets information about all .assets files found
    /// </summary>
    /// <param name="directoryPath">Directory to search in</param>
    /// <param name="recursive">Whether to search recursively</param>
    /// <returns>Array of AssetsFileInfo objects</returns>
    public static AssetsFileInfo[] GetAssetsFileInfo(string directoryPath, bool recursive = true)
    {
        var files = FindAssetsFiles(directoryPath, recursive);
        var fileInfos = new List<AssetsFileInfo>();
        
        foreach (var file in files)
        {
            try
            {
                var fileInfo = new FileInfo(file);
                fileInfos.Add(new AssetsFileInfo
                {
                    FullPath = file,
                    FileName = fileInfo.Name,
                    Directory = fileInfo.DirectoryName ?? string.Empty,
                    Size = fileInfo.Length,
                    LastModified = fileInfo.LastWriteTime
                });
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Warning, $"Failed to get info for file {file}: {ex.Message}");
            }
        }
        
        return fileInfos.ToArray();
    }
}

/// <summary>
/// Information about an assets file
/// </summary>
public class AssetsFileInfo
{
    public string FullPath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Directory { get; set; } = string.Empty;
    public long Size { get; set; }
    public DateTime LastModified { get; set; }
    
    public override string ToString()
    {
        return $"{FileName} ({Size:N0} bytes) - {LastModified:yyyy-MM-dd HH:mm:ss}";
    }
}
