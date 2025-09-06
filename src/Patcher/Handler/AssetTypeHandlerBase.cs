using AssetsTools.NET.Extra;
namespace WMO.AssetPatcher;

public abstract class AssetTypeHandlerBase(AssetClassID classId, params string[] extensions)
{
	public AssetClassID ClassId { get; } = classId;
	public string[] Extensions { get; } = extensions;
}