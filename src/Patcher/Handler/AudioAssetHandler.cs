using AssetsTools.NET;
using AssetsTools.NET.Extra;
using WMO.Helper;
using WMO.Logging;
using NAudio.Wave;
using NAudio.Vorbis;

namespace WMO.AssetPatcher;

public class AudioAssetHandler : AssetTypeHandlerBase
{
    public AudioAssetHandler() : base(AssetClassID.AudioClip, ".wav", ".ogg", ".mp3") { }

    public static IAssetReplacer? CreateReplacer(AssetsManager am,
                                                 AssetsFileInstance fileInst,
                                                 AssetFileInfo assetInfo,
                                                 string assetName,
                                                 byte[] data)
    {
        try
        {
            Logger.Log(LogLevel.Debug, $"Creating replacer for audio asset: {assetName}");
            Logger.Log(LogLevel.Debug, $"Input data size: {data.Length} bytes");
            
            // Variable declarations
            string resourcePath;
            AssetTypeValueField resourceField;
            string assetsFilePath;
            string assetsDirectory;
            string temporalPath;
            string ext = Path.GetExtension(assetName).ToLowerInvariant();
            Logger.Log(LogLevel.Debug, $"File extension detected: {ext}");
            string externalFilePath;
            long newOffset;

            // Audio Data Processing
            Logger.Log(LogLevel.Debug, $"Processing audio data for format: {ext}");
            byte[] writeData = data;
            int channels = 0;
            int sampleRate = 0;
            float duration = 0;
            int bitsPerSample = 0;

            // Read audio data to get metadata
            Logger.Log(LogLevel.Debug, $"Reading audio metadata...");
            using (var ms = new MemoryStream(data))
            {
                WaveStream? reader = null;
                if (ext == ".wav")
                {
                    Logger.Log(LogLevel.Debug, $"Processing as WAV file");
                    reader = new WaveFileReader(ms);
                    // For WAV files, keep the original data with headers for Unity
                    writeData = data; // Use original WAV data, not raw PCM
                }
                else if (ext == ".ogg") 
                {
                    Logger.Log(LogLevel.Debug, $"Processing as OGG Vorbis file");
                    reader = new VorbisWaveReader(ms);
                    writeData = data; // Use original OGG data
                }
                else if (ext == ".mp3") 
                {
                        Logger.Log(LogLevel.Debug, $"Processing as MP3 file");
                    reader = new Mp3FileReader(ms);
                    writeData = data; // Use original MP3 data
                }
                else 
                {
                    Logger.Log(LogLevel.Warning, $"Unsupported audio format: {ext}");
                    return null;
                }

                channels = reader.WaveFormat.Channels;
                sampleRate = reader.WaveFormat.SampleRate;
                duration = (float)reader.TotalTime.TotalSeconds;
                bitsPerSample = reader.WaveFormat.BitsPerSample;
                
                Logger.Log(LogLevel.Debug, $"Audio metadata extracted:");
                Logger.Log(LogLevel.Debug, $"  Channels: {channels}");
                Logger.Log(LogLevel.Debug, $"  Sample Rate: {sampleRate} Hz");
                Logger.Log(LogLevel.Debug, $"  Duration: {duration:F2} seconds");
                Logger.Log(LogLevel.Debug, $"  Bits per Sample: {bitsPerSample}");
                
                reader.Dispose();
            }

            Logger.Log(LogLevel.Debug, $"Reading asset base field...");
            var baseField = am.GetBaseField(fileInst, assetInfo);
            var name = baseField["m_Name"].AsString;
            Logger.Log(LogLevel.Debug, $"Asset name from file: '{name}'");
            Logger.Log(LogLevel.Debug, $"Expected name: '{Path.GetFileNameWithoutExtension(assetName)}'");
            
            if (name != Path.GetFileNameWithoutExtension(assetName))
            {
                Logger.Log(LogLevel.Debug, $"Asset name mismatch, skipping");
                return null;
            }

            Logger.Log(LogLevel.Debug, $"Asset name matches, proceeding with replacement");
            
            // Found the asset to replace
            resourceField = baseField["m_Resource"];
            resourcePath = resourceField["m_Source"].AsString;
            Logger.Log(LogLevel.Debug, $"Current resource path: '{resourcePath}'");
            
            assetsFilePath = fileInst.path;
            assetsDirectory = Path.GetDirectoryName(assetsFilePath) ?? "";
            Logger.Log(LogLevel.Debug, $"Assets directory: {assetsDirectory}");

            // Update metadata
            Logger.Log(LogLevel.Debug, $"Updating asset metadata...");
            baseField["m_Length"].AsFloat = duration;
            baseField["m_Channels"].AsInt = channels;
            baseField["m_Frequency"].AsInt = sampleRate;
            baseField["m_BitsPerSample"].AsInt = bitsPerSample;
            
            // Set correct compression format for Unity
            int compressionFormat;
            if (ext == ".wav")
                compressionFormat = 0; // PCM
            else if (ext == ".ogg")
                compressionFormat = 1; // Vorbis
            else if (ext == ".mp3")
                compressionFormat = 3; // MP3
            else
                compressionFormat = 0; // Default to PCM
            
            Logger.Log(LogLevel.Debug, $"Setting compression format to: {compressionFormat} ({ext})");
            baseField["m_CompressionFormat"].AsInt = compressionFormat;
            
            // Set load type (0 = Decompress On Load, 1 = Compressed In Memory, 2 = Streaming)
            Logger.Log(LogLevel.Debug, $"Setting load type to: Decompress On Load");
            baseField["m_LoadType"].AsInt = 0; // Decompress On Load is safest
            
            // Set other Unity AudioClip properties if they exist
            Logger.Log(LogLevel.Debug, $"Setting additional AudioClip properties...");
            try {
                var preloadField = baseField["m_PreloadAudioData"];
                if (preloadField != null) 
                {
                    preloadField.AsBool = true;
                    Logger.Log(LogLevel.Debug, $"Set m_PreloadAudioData to true");
                }
            } catch { Logger.Log(LogLevel.Debug, $"m_PreloadAudioData field not found, skipping"); }
            
            try {
                var backgroundField = baseField["m_LoadInBackground"];
                if (backgroundField != null) 
                {
                    backgroundField.AsBool = false;
                    Logger.Log(LogLevel.Debug, $"Set m_LoadInBackground to false");
                }
            } catch { Logger.Log(LogLevel.Debug, $"m_LoadInBackground field not found, skipping"); }
            
            try {
                var legacy3DField = baseField["m_Legacy3D"];
                if (legacy3DField != null) 
                {
                    legacy3DField.AsBool = false;
                    Logger.Log(LogLevel.Debug, $"Set m_Legacy3D to false");
                }
            } catch { Logger.Log(LogLevel.Debug, $"m_Legacy3D field not found, skipping"); }

            // Resource file handling (write new audio data)
            Logger.Log(LogLevel.Debug, $"Handling resource file...");
            if (string.IsNullOrEmpty(resourcePath))
            {
                // Generate new .resS file if none exists
                var resourceName = Path.GetFileNameWithoutExtension(assetsFilePath) + ".resS";
                Logger.Log(LogLevel.Debug, $"No existing resource file, creating new: {resourceName}");
                externalFilePath = Path.Combine(assetsDirectory, resourceName);
                temporalPath = externalFilePath + ".temp";
                
                // Update the resource path in the asset
                baseField["m_Resource.m_Source"].AsString = resourceName;
                Logger.Log(LogLevel.Debug, $"Updated resource source to: {resourceName}");
            }
            else
            {
                Logger.Log(LogLevel.Debug, $"Using existing resource file: {resourcePath}");
                externalFilePath = Path.Combine(assetsDirectory, resourcePath);
                temporalPath = externalFilePath + ".temp";
                if (File.Exists(externalFilePath)) 
                {
                    Logger.Log(LogLevel.Debug, $"Copying existing resource file to temp location");
                    File.Copy(externalFilePath, temporalPath, true);
                }
                else
                {
                    Logger.Log(LogLevel.Debug, $"Existing resource file not found, will create new");
                }
            }

            Logger.Log(LogLevel.Debug, $"Writing audio data to resource file...");
            Logger.Log(LogLevel.Debug, $"Resource file path: {externalFilePath}");
            Logger.Log(LogLevel.Debug, $"Temp file path: {temporalPath}");
            
            using (var stream = new FileStream(temporalPath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                newOffset = stream.Length;
                Logger.Log(LogLevel.Debug, $"Current file size: {newOffset} bytes");
                
                // Align to 4-byte boundary for Unity 
                // (idk why we need to do this but fails if not, sooo fuck it)
                if (newOffset % 4 != 0)
                {
                    var padding = 4 - (newOffset % 4);
                    newOffset += padding;
                    Logger.Log(LogLevel.Debug, $"Applying {padding} bytes padding for 4-byte alignment, new offset: {newOffset}");
                }
                
                stream.Position = newOffset;
                stream.Write(writeData, 0, writeData.Length);
                stream.Flush();
                
                Logger.Log(LogLevel.Debug, $"Written {writeData.Length} bytes at offset {newOffset}");
            }
            
            Logger.Log(LogLevel.Debug, $"Replacing temp file with final resource file");
            File.Replace(temporalPath, externalFilePath, null);

            // Add the resource Metadata to the asset file
            Logger.Log(LogLevel.Debug, $"Updating resource metadata in asset...");
            baseField["m_Resource.m_Offset"].AsULong = (ulong)newOffset;
            baseField["m_Resource.m_Size"].AsULong = (ulong)writeData.Length;
            Logger.Log(LogLevel.Debug, $"Resource offset: {newOffset}, size: {writeData.Length}");

            Logger.Log(LogLevel.Debug, $"Creating ContentReplacerFromBuffer...");
            
            // Return a replacer for this asset
            var replacer = new ContentReplacerFromBuffer(baseField.WriteToByteArray());
            var wrapper = new AssetsReplacerWrapper(replacer, assetInfo.PathId);
            Logger.Log(LogLevel.Debug, $"Created replacer wrapper for PathId: {assetInfo.PathId}");
            
            return wrapper;
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, $"Critical error preparing replacer for audio asset '{assetName}': {ex.Message}");
            Logger.Log(LogLevel.Error, $"Exception type: {ex.GetType().FullName}");
            Logger.Log(LogLevel.Error, $"Stack trace: {ex.StackTrace}");
            
            if (ex.InnerException != null)
            {
                Logger.Log(LogLevel.Error, $"Inner exception: {ex.InnerException.Message}");
                Logger.Log(LogLevel.Error, $"Inner exception type: {ex.InnerException.GetType().FullName}");
                Logger.Log(LogLevel.Error, $"Inner exception stack trace: {ex.InnerException.StackTrace}");
            }
            
            ErrorHandler.Handle($"Critical error preparing replacer for audio asset '{assetName}'", ex);
            
            // Re-throw the exception to stop the patching process
            throw new Exception($"Failed to create replacer for audio asset '{assetName}'", ex);
        }
    }
}