using System;
using System.IO;


namespace WMO.AssetManager;
public static class AssetFileFinder
{
    public static string[] FindAssetsFiles(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
            throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");

        var files = Directory.GetFiles(directoryPath, "*.assets");
        if (files.Length == 0)
            throw new FileNotFoundException("No .assets file found in the directory.");

        return files;
    }


}