using System;
using System.IO;
using WMO.Core.Logging;

namespace WMO.Core.Patching;

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
}
