using AssetsTools.NET;
using AssetsTools.NET.Extra;

namespace WMO.Core.Patching;

/// <summary>
/// Wrapper around AssetsTools.NET replacers to implement our IAssetReplacer interface
/// </summary>
public class AssetsReplacerWrapper : IAssetReplacer
{
    private readonly ContentReplacerFromBuffer _replacer;
    private readonly long _pathId;

    public AssetsReplacerWrapper(ContentReplacerFromBuffer replacer, long pathId = 0)
    {
        _replacer = replacer;
        _pathId = pathId;
    }

    public long PathId => _pathId;

    public byte[] GetReplacementData()
    {
        // Placeholder the actual writing is handled by AssetsTools.NET
        return new byte[0]; 
    }

    public ContentReplacerFromBuffer GetAssetsReplacer() => _replacer;
}
