using AssetsTools.NET;
using AssetsTools.NET.Extra;
using WMO.Helper;
using WMO.Logging;
using FileInstance = AssetsTools.NET.Extra.AssetsFileInstance;

namespace WMO.AssetPatcher {
    public static class AssetsFilePatcher {
        private static readonly Dictionary<long, string> _resourcePaths = new();

        private static readonly List<AssetTypeHandlerBase> _assetHandlers =
            [new TextAssetHandler(), new AudioAssetHandler(), new TextureAssetHandler()];


        private static void ReadResourcePaths(string gamePath) {
            try {
                var am = new AssetsManager();
                am.LoadClassDatabase("Resources/cldb_2018.4.6f1.dat");
                
                var ggmPath = Path.Combine(gamePath, "Pathologic_Data", "globalgamemanagers");
                if (!File.Exists(ggmPath)) {
                    Logger.Log(LogLevel.Error, $"Could not find globalgamemanagers at {ggmPath}");
                    return;
                }
                
                var ggm = am.LoadAssetsFile(ggmPath);
                
                var resourceManagerAssets = ggm.file.GetAssetsOfType(AssetClassID.ResourceManager);
                if (resourceManagerAssets.Count == 0) {
                    Logger.Log(LogLevel.Error, $"No ResourceManager asset found in globalgamemanagers");
                    return;
                }
                
                var rsrcInfo = resourceManagerAssets[0];
                var rsrcBf = am.GetBaseField(ggm, rsrcInfo);
                
                var m_Container = rsrcBf["m_Container.Array"];
                
                foreach (var data in m_Container.Children) {
                    var name = data[0].AsString;
                    var pathId = data[1]["m_PathID"].AsLong;
                    
                    _resourcePaths[pathId] = name;
                }
                
                ggm.file.Close();
                am.UnloadAllAssetsFiles();
            } catch (Exception ex) {
                Logger.Log(LogLevel.Error, $"Error reading resource paths: {ex.Message}");
                ErrorHandler.Handle("Failed to read asset paths from globalgamemanagers", ex);
            }
        }
    }
}