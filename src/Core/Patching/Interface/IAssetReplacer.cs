namespace WMO.Core.Patching;

public interface IAssetReplacer
{
    long PathId { get; }
    byte[] GetReplacementData();
}
