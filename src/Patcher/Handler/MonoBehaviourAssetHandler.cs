using AssetsTools.NET;
using AssetsTools.NET.Extra;
using WMO.Helper;
using WMO.Logging;

namespace WMO.AssetPatcher;

public class MonoBehaviourAssetHandler : AssetTypeHandlerBase
{
    public MonoBehaviourAssetHandler() : base(AssetClassID.MonoBehaviour, ".txt", ".json") { }

    public static IAssetReplacer? CreateReplacer(AssetsManager am,
                                                 AssetsFileInstance fileInst,
                                                 AssetFileInfo assetInfo,
                                                 string assetName,
                                                 byte[] data)
    {
        try
        {
            Logger.Log(LogLevel.Debug, $"Creating replacer for MonoBehaviour asset: {assetName}");
            Logger.Log(LogLevel.Debug, $"Input data size: {data.Length} bytes");

            string ext = Path.GetExtension(assetName).ToLowerInvariant();
            Logger.Log(LogLevel.Debug, $"File extension detected: {ext}");

            am.MonoTempGenerator = new MonoCecilTempGenerator(assetName);

            // Read the MonoBehaviour's base field
            Logger.Log(LogLevel.Debug, $"Reading MonoBehaviour base field...");
            var baseField = am.GetBaseField(fileInst, assetInfo);

            var replacer = new ContentReplacerFromBuffer(baseField.WriteToByteArray());
            var wrapper = new AssetsReplacerWrapper(replacer, assetInfo.PathId);

            return wrapper;
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, $"Error preparing replacer for MonoBehaviour asset '{assetName}': {ex.Message}");
            Logger.Log(LogLevel.Debug, $"Exception type: {ex.GetType().Name}");
            Logger.Log(LogLevel.Debug, $"Full stack trace: {ex.StackTrace}");

            if (ex.InnerException != null)
            {
                Logger.Log(LogLevel.Debug, $"Inner exception: {ex.InnerException.Message}");
            }

            ErrorHandler.Handle("Error preparing replacer for MonoBehaviour asset", ex);
            return null;
        }
    }
}
