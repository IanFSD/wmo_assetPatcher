namespace WMO.AssetPatcher;

public interface IAssetReplacer
{
    long PathId { get; }
    byte[] GetReplacementData();
}