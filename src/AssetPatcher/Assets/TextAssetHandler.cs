using AssetsTools.NET;
using AssetsTools.NET.Extra;
using WMO.Helper;
using WMO.Logging;
using FileInstance = AssetsTools.NET.Extra.AssetsFileInstance;

namespace WMO.AssetPatcher;

public class TextAssetHandler() : AssetTypeHandlerBase(AssetClassID.TextAsset, ".bytes", ".txt") {
	public override bool Replace(AssetsManager am, FileInstance fileInst, AssetFileInfo assetInfo, byte[] data) {
		try {
			var baseField = am.GetBaseField(fileInst, assetInfo);
			baseField["m_Script"].AsByteArray = data;
			assetInfo.Replacer = new ContentReplacerFromBuffer(baseField.WriteToByteArray());
			Logger.Log(LogLevel.Info, $"Successfully replaced text asset");
			return true;
		} catch (Exception ex) {
			ErrorHandler.Handle("Error replacing text asset", ex);
			return false;
		}
	}
}