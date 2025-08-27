using AssetsTools.NET;
using AssetsTools.NET.Extra;
using FileInstance = AssetsTools.NET.Extra.AssetsFileInstance;
namespace WMO.AssetPatcher;

public abstract class AssetTypeHandlerBase(AssetClassID classId, params string[] extensions)
{
	public AssetClassID ClassId { get; } = classId;
	public string[] Extensions { get; } = extensions;
}