using AssetsTools.NET;
using AssetsTools.NET.Extra;
using WMO.Core.Logging;
using WMO.Core.Models;
using WMO.Core.Models.Enums;
using WMO.Core.Helpers;

namespace WMO.Core.Services;

/// <summary>
/// Service for scanning Unity asset bundles and discovering assets
/// </summary>
public class AssetScannerService
{
    private readonly List<DiscoveredAsset> _discoveredAssets = new();
    private bool _isScanning = false;
    
    /// <summary>
    /// Event raised when scanning progress changes
    /// </summary>
    public event EventHandler<AssetScanProgressEventArgs>? ProgressChanged;
    
    /// <summary>
    /// Event raised when scanning completes
    /// </summary>
    public event EventHandler<AssetScanCompletedEventArgs>? ScanCompleted;
    
    /// <summary>
    /// All discovered assets
    /// </summary>
    public IReadOnlyList<DiscoveredAsset> DiscoveredAssets => _discoveredAssets.AsReadOnly();
    
    /// <summary>
    /// Whether a scan is currently in progress
    /// </summary>
    public bool IsScanning => _isScanning;
    
    /// <summary>
    /// Scan the game directory for Unity assets
    /// </summary>
    /// <param name="gamePath">Path to the game installation</param>
    /// <param name="filter">Filter to apply during scanning</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task ScanAssetsAsync(string gamePath, AssetScanFilter filter, CancellationToken cancellationToken = default)
    {
        if (_isScanning)
        {
            Logger.Log(LogLevel.Warning, $"Asset scanning is already in progress");
            return;
        }
        
        try
        {
            _isScanning = true;
            _discoveredAssets.Clear();
            
            Logger.Log(LogLevel.Info, $"Starting asset scan in: {gamePath}");
            Logger.Log(LogLevel.Debug, $"Filter settings: Types={filter.IncludedTypes.Count}, ModdableOnly={filter.ModdableOnly}, MinSize={filter.MinSize}");
            
            // Find all asset bundle files
            var assetFiles = await FindAssetFilesAsync(gamePath, cancellationToken);
            
            if (!assetFiles.Any())
            {
                Logger.Log(LogLevel.Warning, $"No asset files found in game directory");
                OnScanCompleted(new AssetScanCompletedEventArgs(true, _discoveredAssets, "No asset files found"));
                return;
            }
            
            Logger.Log(LogLevel.Info, $"Found {assetFiles.Count} asset files to scan");
            
            var totalFiles = assetFiles.Count;
            var processedFiles = 0;
            
            // Create AssetsManager for scanning
            var manager = new AssetsManager();
            
            // Load class package - required for proper asset interpretation
            var classPackagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "lz4.tpk");
            if (!File.Exists(classPackagePath))
            {
                Logger.Log(LogLevel.Error, $"Class package not found: {classPackagePath}");
                OnScanCompleted(new AssetScanCompletedEventArgs(false, _discoveredAssets, "Class package (lz4.tpk) not found"));
                return;
            }
            
            Logger.Log(LogLevel.Debug, $"Loading class package from: {classPackagePath}");
            manager.LoadClassPackage(classPackagePath);
            
            foreach (var assetFile in assetFiles)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    Logger.Log(LogLevel.Info, $"Asset scanning cancelled by user");
                    OnScanCompleted(new AssetScanCompletedEventArgs(false, _discoveredAssets, "Cancelled by user"));
                    return;
                }
                
                try
                {
                    await ScanSingleAssetFileAsync(manager, assetFile, filter, cancellationToken);
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Error, $"Error scanning {Path.GetFileName(assetFile)}: {ex.Message}");
                    // Continue with other files
                }
                
                processedFiles++;
                OnProgressChanged(new AssetScanProgressEventArgs(processedFiles, totalFiles, Path.GetFileName(assetFile)));
                
                // Yield control to prevent UI freezing
                await Task.Yield();
            }
            
            // Sort assets by name for better display
            _discoveredAssets.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            
            Logger.Log(LogLevel.Info, $"Asset scanning completed. Found {_discoveredAssets.Count} assets matching filter criteria");
            OnScanCompleted(new AssetScanCompletedEventArgs(true, _discoveredAssets));
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, $"Error during asset scanning: {ex.Message}");
            OnScanCompleted(new AssetScanCompletedEventArgs(false, _discoveredAssets, ex.Message));
        }
        finally
        {
            _isScanning = false;
        }
    }
    
    /// <summary>
    /// Find asset bundle files in the game directory
    /// </summary>
    private async Task<List<string>> FindAssetFilesAsync(string gamePath, CancellationToken cancellationToken)
    {
        var assetFiles = new List<string>();
        
        // Unity asset file extensions that we need to scan
        var extensions = new[] { "*.assets", "*.resource", "*.ress" };
        
        if (!Directory.Exists(gamePath))
        {
            Logger.Log(LogLevel.Warning, $"Game directory not found: {gamePath}");
            return assetFiles;
        }
        
        Logger.Log(LogLevel.Debug, $"Searching for asset files in: {gamePath} (recursive)");
        
        await Task.Run(() =>
        {
            foreach (var extension in extensions)
            {
                if (cancellationToken.IsCancellationRequested) break;
                
                try
                {
                    var files = Directory.GetFiles(gamePath, extension, SearchOption.AllDirectories);
                    assetFiles.AddRange(files);
                    Logger.Log(LogLevel.Debug, $"Extension {extension}: found {files.Length} files");
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Warning, $"Error searching with extension {extension}: {ex.Message}");
                }
            }
        }, cancellationToken);
        
        return assetFiles.Distinct().ToList();
    }
    
    /// <summary>
    /// Scan a single asset file
    /// </summary>
    private async Task ScanSingleAssetFileAsync(AssetsManager manager, string assetFilePath, AssetScanFilter filter, CancellationToken cancellationToken)
    {
        Logger.Log(LogLevel.Debug, $"Scanning asset file: {Path.GetFileName(assetFilePath)}");
        
        await Task.Run(() =>
        {
            try
            {
                // Load the asset file
                var afileInst = manager.LoadAssetsFile(assetFilePath, false);
                var afile = afileInst.file;
                
                Logger.Log(LogLevel.Debug, $"Loaded asset file: {afile.Metadata.UnityVersion}, {afile.AssetInfos.Count} assets");
                
                // Load class database for this Unity version
                try
                {
                    manager.LoadClassDatabaseFromPackage(afile.Metadata.UnityVersion);
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Warning, $"Could not load class database for Unity {afile.Metadata.UnityVersion}: {ex.Message}");
                    return;
                }
                
                // Scan all assets in this file
                foreach (var assetInfo in afile.AssetInfos)
                {
                    if (cancellationToken.IsCancellationRequested) break;
                    
                    try
                    {
                        var asset = CreateDiscoveredAsset(manager, afileInst, assetInfo, assetFilePath);
                        
                        if (asset != null && filter.PassesFilter(asset))
                        {
                            _discoveredAssets.Add(asset);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(LogLevel.Trace, $"Error processing asset {assetInfo.PathId}: {ex.Message}");
                        // Continue with other assets
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error, $"Error loading asset file {Path.GetFileName(assetFilePath)}: {ex.Message}");
            }
        }, cancellationToken);
    }
    
    /// <summary>
    /// Create a DiscoveredAsset from asset info
    /// </summary>
    private DiscoveredAsset? CreateDiscoveredAsset(AssetsManager manager, AssetsFileInstance fileInst, AssetFileInfo assetInfo, string assetFilePath)
    {
        try
        {
            // Get basic asset information
            var classId = (AssetClassID)assetInfo.TypeId;
            
            // Try to map to our UnityAssetType enum
            var unityAssetType = TryGetUnityAssetType(classId);
            if (unityAssetType == null) return null;
            
            var asset = new DiscoveredAsset
            {
                PathId = assetInfo.PathId,
                AssetType = unityAssetType.Value,
                ClassId = assetInfo.TypeId,
                Size = assetInfo.ByteSize,
                FilePath = assetFilePath
            };
            
            // Try to get asset name
            try
            {
                var baseField = manager.GetBaseField(fileInst, assetInfo);
                var nameField = baseField["m_Name"];
                
                if (nameField != null && !nameField.IsDummy)
                {
                    asset.Name = nameField.AsString;
                }
                else
                {
                    asset.Name = $"Asset_{assetInfo.PathId}";
                }
                
                // Try to extract additional properties for certain asset types
                ExtractAdditionalProperties(asset, baseField);
            }
            catch
            {
                // If we can't read the asset, just use a default name
                asset.Name = $"{unityAssetType}_{assetInfo.PathId}";
            }
            
            return asset;
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Trace, $"Could not create asset for PathId {assetInfo.PathId}: {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Try to map AssetClassID to UnityAssetType
    /// </summary>
    private UnityAssetType? TryGetUnityAssetType(AssetClassID classId)
    {
        // Try to find matching UnityAssetType by comparing ClassId values
        foreach (var assetType in Enum.GetValues<UnityAssetType>())
        {
            if (assetType.GetClassId() == classId)
            {
                return assetType;
            }
        }
        
        // If no exact match, return null to filter out unknown types
        return null;
    }
    
    /// <summary>
    /// Extract additional properties specific to certain asset types
    /// </summary>
    private void ExtractAdditionalProperties(DiscoveredAsset asset, AssetTypeValueField baseField)
    {
        try
        {
            switch (asset.AssetType)
            {
                case UnityAssetType.Texture2D:
                    ExtractTexture2DProperties(asset, baseField);
                    break;
                    
                case UnityAssetType.AudioClip:
                    ExtractAudioClipProperties(asset, baseField);
                    break;
                    
                case UnityAssetType.Mesh:
                    ExtractMeshProperties(asset, baseField);
                    break;
                    
                case UnityAssetType.Material:
                    ExtractMaterialProperties(asset, baseField);
                    break;
            }
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Trace, $"Could not extract properties for {asset.Name}: {ex.Message}");
        }
    }
    
    private void ExtractTexture2DProperties(DiscoveredAsset asset, AssetTypeValueField baseField)
    {
        try
        {
            var width = baseField["m_Width"]?.AsInt ?? 0;
            var height = baseField["m_Height"]?.AsInt ?? 0;
            var format = baseField["m_TextureFormat"]?.AsInt ?? 0;
            
            asset.Properties["Width"] = width;
            asset.Properties["Height"] = height;
            asset.Properties["Format"] = format;
            asset.Properties["Resolution"] = $"{width}x{height}";
        }
        catch { }
    }
    
    private void ExtractAudioClipProperties(DiscoveredAsset asset, AssetTypeValueField baseField)
    {
        try
        {
            var channels = baseField["m_Channels"]?.AsInt ?? 0;
            var frequency = baseField["m_Frequency"]?.AsInt ?? 0;
            var length = baseField["m_Length"]?.AsFloat ?? 0f;
            
            asset.Properties["Channels"] = channels;
            asset.Properties["Frequency"] = frequency;
            asset.Properties["Length"] = length;
            asset.Properties["Duration"] = $"{length:F2}s";
        }
        catch { }
    }
    
    private void ExtractMeshProperties(DiscoveredAsset asset, AssetTypeValueField baseField)
    {
        try
        {
            var vertices = baseField["m_VertexData"]?["m_VertexCount"]?.AsUInt ?? 0;
            var indexBuffer = baseField["m_IndexBuffer"]?.AsArray;
            var triangles = indexBuffer?.size ?? 0;
            
            asset.Properties["Vertices"] = vertices;
            asset.Properties["Triangles"] = triangles / 3; // 3 indices per triangle
        }
        catch { }
    }
    
    private void ExtractMaterialProperties(DiscoveredAsset asset, AssetTypeValueField baseField)
    {
        try
        {
            var shader = baseField["m_Shader"];
            if (shader != null && !shader.IsDummy)
            {
                // This is a reference to another asset, would need more work to resolve
                asset.Properties["HasShader"] = true;
            }
        }
        catch { }
    }
    
    /// <summary>
    /// Clear all discovered assets
    /// </summary>
    public void ClearResults()
    {
        _discoveredAssets.Clear();
    }
    
    /// <summary>
    /// Get asset statistics
    /// </summary>
    public AssetStatistics GetStatistics()
    {
        var stats = new AssetStatistics();
        
        foreach (var asset in _discoveredAssets)
        {
            stats.TotalAssets++;
            stats.TotalSize += asset.Size;
            
            if (stats.AssetTypeCounts.ContainsKey(asset.AssetType))
                stats.AssetTypeCounts[asset.AssetType]++;
            else
                stats.AssetTypeCounts[asset.AssetType] = 1;
        }
        
        return stats;
    }
    
    protected virtual void OnProgressChanged(AssetScanProgressEventArgs e)
    {
        ProgressChanged?.Invoke(this, e);
    }
    
    protected virtual void OnScanCompleted(AssetScanCompletedEventArgs e)
    {
        ScanCompleted?.Invoke(this, e);
    }
}

/// <summary>
/// Event args for scan progress updates
/// </summary>
public class AssetScanProgressEventArgs : EventArgs
{
    public int ProcessedFiles { get; }
    public int TotalFiles { get; }
    public string CurrentFile { get; }
    public int PercentComplete => TotalFiles > 0 ? (ProcessedFiles * 100) / TotalFiles : 0;
    
    public AssetScanProgressEventArgs(int processedFiles, int totalFiles, string currentFile)
    {
        ProcessedFiles = processedFiles;
        TotalFiles = totalFiles;
        CurrentFile = currentFile;
    }
}

/// <summary>
/// Event args for scan completion
/// </summary>
public class AssetScanCompletedEventArgs : EventArgs
{
    public bool Success { get; }
    public IReadOnlyList<DiscoveredAsset> Assets { get; }
    public string? ErrorMessage { get; }
    
    public AssetScanCompletedEventArgs(bool success, IReadOnlyList<DiscoveredAsset> assets, string? errorMessage = null)
    {
        Success = success;
        Assets = assets;
        ErrorMessage = errorMessage;
    }
}

/// <summary>
/// Statistics about discovered assets
/// </summary>
public class AssetStatistics
{
    public int TotalAssets { get; set; }
    public long TotalSize { get; set; }
    public Dictionary<UnityAssetType, int> AssetTypeCounts { get; set; } = new();
    
    public string FormattedTotalSize
    {
        get
        {
            if (TotalSize >= 1024 * 1024 * 1024) // GB
                return $"{TotalSize / (1024.0 * 1024.0 * 1024.0):F2} GB";
            if (TotalSize >= 1024 * 1024) // MB
                return $"{TotalSize / (1024.0 * 1024.0):F2} MB";
            if (TotalSize >= 1024) // KB
                return $"{TotalSize / 1024.0:F2} KB";
            return $"{TotalSize} B";
        }
    }
}
