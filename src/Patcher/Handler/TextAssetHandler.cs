using AssetsTools.NET;
using AssetsTools.NET.Extra;
using WMO.Helper;
using WMO.Logging;
using FileInstance = AssetsTools.NET.Extra.AssetsFileInstance;

namespace WMO.AssetPatcher;

public class TextAssetHandler() : AssetTypeHandlerBase(AssetClassID.TextAsset, ".bytes", ".txt")
{
	public bool Replace(AssetsManager am, FileInstance fileInst, AssetFileInfo assetInfo, byte[] data)
	{
		try
		{
			return false;
		}
		catch (Exception ex)
		{
			ErrorHandler.Handle("Error replacing text asset", ex);
			return false;
		}
	}

}