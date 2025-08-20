using AssetsTools.NET;
using AssetsTools.NET.Extra;
using WMO.Helper;
using WMO.Logging;


namespace WMO.AssetPatcher;

public static class AssetPatcher
{
    private const string MANAGED_PATH = "Whisper Mountain Outbreak_Data/Managed/";
    private const string ASSETS_PATH = "Whisper Mountain Outbreak_Data/";
    private const string DATA_PATH = "Data/";

    public static bool TryPatch()
    {
        try
        {

            if (!BackupManager.TryRecoverBackups()) return false;

            Logger.Log(LogLevel.Info, $"Preparing to patch...");

            Logger.Log(LogLevel.Info, $"Finished loading  mods.");
            SettingsHolder.IsPatched = true;
            return true;
        }
        catch (Exception ex)
        {
            ErrorHandler.Handle("Error during patching", ex);
            return false;
        }
    }
    
    public static bool PatchAudioAssets(string assetsFilePath, string modAudioFolder)
    {
        try
        {
            var manager = new AssetsManager();
            var fileInst = manager.LoadAssetsFile(assetsFilePath);
            manager.LoadClassDatabase("Resources/cldb_2018.4.6f1.dat");

            var audioHandler = new AudioAssetHandler();

            foreach (var file in Directory.GetFiles(modAudioFolder))
            {
                var extension = Path.GetExtension(file).ToLower();
                if (!audioHandler.Extensions.Contains(extension))
                    continue;

                var assetName = Path.GetFileNameWithoutExtension(file);
                var assetData = File.ReadAllBytes(file);

                // Find the asset in the assets file
                var assetInfo = fileInst.file.GetAssetsOfType(audioHandler.ClassId)
                    .FirstOrDefault(a =>
                        manager.GetBaseField(fileInst, a)["m_Name"].AsString == assetName);

                if (assetInfo == null)
                {
                    Logger.Log(LogLevel.Error, $"Audio asset '{assetName}' not found in assets file.");
                    continue;
                }

                var replaced = audioHandler.Replace(manager, fileInst, assetInfo, assetData);
                if (replaced)
                    Logger.Log(LogLevel.Info, $"Patched audio asset: {assetName}");
                else
                    Logger.Log(LogLevel.Error, $"Failed to patch audio asset: {assetName}");
            }

            // Save changes
            var outputFile = assetsFilePath + ".new";
            using (var writer = new AssetsFileWriter(outputFile))
                fileInst.file.Write(writer, -1);

            fileInst.file.Close();
            BackupManager.CreateBackup(assetsFilePath);
            File.Copy(outputFile, assetsFilePath, true);
            File.Delete(outputFile);

            Logger.Log(LogLevel.Info, $"Audio assets patching complete.");
            return true;
        }
        catch (Exception ex)
        {
            ErrorHandler.Handle("Error patching audio assets", ex);
            return false;
        }
    }

}