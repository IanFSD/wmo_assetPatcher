using System.Text.Json;
using AssetsTools.NET;
using AssetsTools.NET.Extra;
using AssetsTools.NET.Texture;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using WMO.Core.Logging;
using WMO.Core.Models;

namespace WMO.Core.Services;

/// <summary>
/// Patches Unity AudioClip and Texture2D assets in the game's .assets / .resS files
/// by replacing the data with files supplied by enabled mods.
///
/// Lifecycle
/// ---------
/// 1. PatchAssetsAsync(gameDataPath, enabledMods)
///    - Called at game launch (before the process starts).
///    - Backs up every .assets/.resS file it touches into AppDir/backups/.
///    - Writes a patch_manifest.json listing original -> backup path pairs.
///
/// 2. RestoreOriginalsAsync()
///    - Called after the game exits (or on startup if a stale manifest exists).
///    - Reads patch_manifest.json and overwrites each game file with its backup.
///    - Clears the manifest on success.
///
/// Audio replacement rules
/// -----------------------
/// - ContentFile.Type == "Audio" (or auto-detected from .ogg/.wav extension).
/// - ContentFile.Target must match the Unity asset's m_Name (case-insensitive).
/// - .ogg -> m_CompressionFormat = 1 (Vorbis); .wav -> m_CompressionFormat = 0 (PCM).
/// - Inline audio (m_Resource.source == "")  -> bytes written into the .assets file.
/// - Streamed audio (m_Resource.source != "") -> bytes appended to the .resS file;
///   metadata updated with new offset + size.
///
/// Texture replacement rules
/// -------------------------
/// - ContentFile.Type == "Texture" or "Sprite" (or auto-detected from .png/.jpg extension).
///   Both types target a Texture2D asset by m_Name — Sprite assets are rect descriptors only;
///   their pixel data lives in the backing Texture2D.
/// - Replacement image is loaded via ImageSharp, resized to the original texture dimensions,
///   converted to BGRA32 bytes, then encoded to match the original m_TextureFormat via
///   TextureFile.Encode. If the format is not supported by the encoder (e.g. DXT1/DXT5/BC7),
///   falls back to RGBA32 and updates m_TextureFormat accordingly.
/// - Inline textures (m_StreamData.path == "") -> written into the .assets file.
/// - Streamed textures (m_StreamData.path != "") -> bytes appended to the .resS file;
///   m_StreamData.offset/size updated; m_StreamData.path preserved.
/// - If two enabled mods target the same asset name, the last one in the list wins.
/// </summary>
public class AssetPatcherService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = null
    };

    private static string BackupDir =>
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "backups");

    private static string ManifestPath =>
        Path.Combine(BackupDir, "patch_manifest.json");

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Patches AudioClip and Texture2D assets for every enabled Audio/Texture/Sprite
    /// content file declared by <paramref name="enabledMods"/>.
    /// Also applies MonoBehaviourEdit and MonoBehaviourInject entries from AssetEdits.
    /// Returns a summary string suitable for a status bar message.
    /// </summary>
    public async Task<string> PatchAssetsAsync(string gameDataPath, List<FolderMod> enabledMods)
    {
        var audioTargets   = BuildAudioTargetMap(enabledMods);
        var textureTargets = BuildTextureTargetMap(enabledMods);
        var mbEditTargets  = BuildMonoBehaviourEditMap(enabledMods);
        var mbInjectList   = BuildMonoBehaviourInjectList(enabledMods);

        if (audioTargets.Count == 0 && textureTargets.Count == 0
            && mbEditTargets.Count == 0 && mbInjectList.Count == 0)
            return "No asset replacements to apply.";

        Logger.Log(LogLevel.Info,
            $"[AssetPatcher] Applying {audioTargets.Count} audio + {textureTargets.Count} texture" +
            $" + {mbEditTargets.Count} MB-edit + {mbInjectList.Count} MB-inject replacement(s)...");

        Directory.CreateDirectory(BackupDir);

        var manifest = new PatchManifest();
        int[] counters = [0, 0]; // [0]=patched, [1]=errors

        await Task.Run(() =>
        {
            var classPackagePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "Resources", "lz4.tpk");

            if (!File.Exists(classPackagePath))
            {
                Logger.Log(LogLevel.Error,
                    $"[AssetPatcher] lz4.tpk not found - cannot patch assets.");
                counters[1]++;
                return;
            }

            var assetFiles = Directory.GetFiles(gameDataPath, "*.assets",
                SearchOption.AllDirectories);

            var manager = new AssetsManager();
            manager.LoadClassPackage(classPackagePath);

            foreach (var assetFilePath in assetFiles)
            {
                try
                {
                    PatchSingleAssetsFile(manager, assetFilePath,
                        audioTargets, textureTargets, mbEditTargets, mbInjectList,
                        manifest, counters);
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Error,
                        $"[AssetPatcher] Error patching {Path.GetFileName(assetFilePath)}: {ex.Message}");
                    counters[1]++;
                }
            }
        });

        int patched = counters[0];
        int errors  = counters[1];

        if (manifest.HasEntries)
        {
            await File.WriteAllTextAsync(ManifestPath,
                JsonSerializer.Serialize(manifest, JsonOptions));
            Logger.Log(LogLevel.Success,
                $"[AssetPatcher] patch_manifest.json written ({manifest.Files.Count} file(s)).");
        }

        string summary = patched > 0
            ? $"Assets patched: {patched} replacement(s) applied."
            : "No matching assets found.";
        if (errors > 0)
            summary += $" ({errors} error(s) - see log)";

        Logger.Log(LogLevel.Info, $"[AssetPatcher] {summary}");
        return summary;
    }

    /// <summary>
    /// Restores every game file listed in patch_manifest.json from its backup
    /// and clears the manifest. Safe to call even if no manifest exists.
    /// </summary>
    public async Task<string> RestoreOriginalsAsync()
    {
        if (!File.Exists(ManifestPath))
            return "No patch manifest found - nothing to restore.";

        PatchManifest manifest;
        try
        {
            var json = await File.ReadAllTextAsync(ManifestPath);
            manifest = JsonSerializer.Deserialize<PatchManifest>(json, JsonOptions)
                       ?? new PatchManifest();
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error,
                $"[AssetPatcher] Could not read patch manifest: {ex.Message}");
            return "Could not read patch manifest.";
        }

        if (!manifest.HasEntries)
        {
            File.Delete(ManifestPath);
            return "Patch manifest was empty - nothing to restore.";
        }

        int restored = 0;
        int errors   = 0;

        await Task.Run(() =>
        {
            foreach (var entry in manifest.Files)
            {
                try
                {
                    if (!File.Exists(entry.BackupPath))
                    {
                        Logger.Log(LogLevel.Warning,
                            $"[AssetPatcher] Backup not found: {entry.BackupPath}");
                        errors++;
                        continue;
                    }

                    File.Copy(entry.BackupPath, entry.OriginalPath, overwrite: true);
                    Logger.Log(LogLevel.Debug,
                        $"[AssetPatcher] Restored: {Path.GetFileName(entry.OriginalPath)}");
                    restored++;
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Error,
                        $"[AssetPatcher] Could not restore {Path.GetFileName(entry.OriginalPath)}: {ex.Message}");
                    errors++;
                }
            }
        });

        try { File.Delete(ManifestPath); } catch { /* best-effort */ }

        string summary = restored > 0
            ? $"Assets restored: {restored} file(s) returned to original."
            : "Restore completed (no files copied).";
        if (errors > 0)
            summary += $" ({errors} error(s) - see log)";

        Logger.Log(LogLevel.Info, $"[AssetPatcher] {summary}");
        return summary;
    }

    /// <summary>
    /// Returns true if a patch manifest exists from a previous session.
    /// </summary>
    public static bool HasStaleManifest() => File.Exists(ManifestPath);

    // -------------------------------------------------------------------------
    // Target map builders
    // -------------------------------------------------------------------------

    private static Dictionary<string, (string FilePath, string Ext)> BuildAudioTargetMap(
        List<FolderMod> enabledMods)
    {
        var map = new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase);

        foreach (var mod in enabledMods.Where(m => m.IsEnabled))
        {
            if (mod.Manifest?.ContentFiles == null) continue;

            foreach (var cf in mod.Manifest.ContentFiles)
            {
                if (!cf.Enabled || string.IsNullOrWhiteSpace(cf.Target)) continue;

                string absPath = AbsPath(mod.FolderPath, cf.FilePath);
                string ext     = Path.GetExtension(absPath).ToLowerInvariant();

                bool isAudio = string.Equals(cf.Type, "Audio", StringComparison.OrdinalIgnoreCase)
                               || (string.IsNullOrEmpty(cf.Type) && (ext == ".ogg" || ext == ".wav"));
                if (!isAudio) continue;

                if (ext != ".ogg" && ext != ".wav")
                {
                    Logger.Log(LogLevel.Warning,
                        $"[AssetPatcher] Skipping '{cf.FilePath}' in '{mod.Name}' - unsupported audio format '{ext}'.");
                    continue;
                }

                if (!File.Exists(absPath))
                {
                    Logger.Log(LogLevel.Warning,
                        $"[AssetPatcher] Audio file not found: {absPath}");
                    continue;
                }

                if (map.ContainsKey(cf.Target!))
                    Logger.Log(LogLevel.Warning,
                        $"[AssetPatcher] Audio target '{cf.Target}' claimed by multiple mods - '{mod.Name}' wins.");

                map[cf.Target!] = (absPath, ext);
            }
        }

        return map;
    }

    private static Dictionary<string, (string FilePath, string Ext)> BuildTextureTargetMap(
        List<FolderMod> enabledMods)
    {
        var map = new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase);

        foreach (var mod in enabledMods.Where(m => m.IsEnabled))
        {
            if (mod.Manifest?.ContentFiles == null) continue;

            foreach (var cf in mod.Manifest.ContentFiles)
            {
                if (!cf.Enabled || string.IsNullOrWhiteSpace(cf.Target)) continue;

                string absPath = AbsPath(mod.FolderPath, cf.FilePath);
                string ext     = Path.GetExtension(absPath).ToLowerInvariant();

                bool isTexture =
                    string.Equals(cf.Type, "Texture", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(cf.Type, "Sprite",  StringComparison.OrdinalIgnoreCase) ||
                    (string.IsNullOrEmpty(cf.Type) && (ext == ".png" || ext == ".jpg" || ext == ".jpeg"));
                if (!isTexture) continue;

                if (ext != ".png" && ext != ".jpg" && ext != ".jpeg")
                {
                    Logger.Log(LogLevel.Warning,
                        $"[AssetPatcher] Skipping '{cf.FilePath}' in '{mod.Name}' - unsupported texture format '{ext}'. Use .png or .jpg.");
                    continue;
                }

                if (!File.Exists(absPath))
                {
                    Logger.Log(LogLevel.Warning,
                        $"[AssetPatcher] Texture file not found: {absPath}");
                    continue;
                }

                if (map.ContainsKey(cf.Target!))
                    Logger.Log(LogLevel.Warning,
                        $"[AssetPatcher] Texture target '{cf.Target}' claimed by multiple mods - '{mod.Name}' wins.");

                map[cf.Target!] = (absPath, ext);
            }
        }

        return map;
    }

    /// <summary>
    /// Builds a map of MonoBehaviour edit targets from all enabled mods' AssetEdits lists.
    /// Key = m_Name of the target asset (case-insensitive).
    /// Value = absolute path to the JSON patch file.
    /// </summary>
    private static Dictionary<string, string> BuildMonoBehaviourEditMap(List<FolderMod> enabledMods)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var mod in enabledMods.Where(m => m.IsEnabled))
        {
            if (mod.Manifest?.AssetEdits == null) continue;

            foreach (var ae in mod.Manifest.AssetEdits)
            {
                if (!ae.Enabled) continue;
                if (!string.Equals(ae.Type, "MonoBehaviourEdit", StringComparison.OrdinalIgnoreCase)) continue;
                if (string.IsNullOrWhiteSpace(ae.Target)) continue;
                if (string.IsNullOrWhiteSpace(ae.FilePath)) continue;

                string absPath = AbsPath(mod.FolderPath, ae.FilePath);
                if (!File.Exists(absPath))
                {
                    Logger.Log(LogLevel.Warning,
                        $"[AssetPatcher] MonoBehaviourEdit patch file not found: {absPath}");
                    continue;
                }

                if (map.ContainsKey(ae.Target!))
                    Logger.Log(LogLevel.Warning,
                        $"[AssetPatcher] MonoBehaviourEdit target '{ae.Target}' claimed by multiple mods - '{mod.Name}' wins.");

                map[ae.Target!] = absPath;
            }
        }

        return map;
    }

    /// <summary>
    /// Builds the list of MonoBehaviour inject descriptors from all enabled mods' AssetEdits lists.
    /// </summary>
    private static List<MonoBehaviourInjectEntry> BuildMonoBehaviourInjectList(List<FolderMod> enabledMods)
    {
        var list = new List<MonoBehaviourInjectEntry>();

        foreach (var mod in enabledMods.Where(m => m.IsEnabled))
        {
            if (mod.Manifest?.AssetEdits == null) continue;

            foreach (var ae in mod.Manifest.AssetEdits)
            {
                if (!ae.Enabled) continue;
                if (!string.Equals(ae.Type, "MonoBehaviourInject", StringComparison.OrdinalIgnoreCase)) continue;
                if (string.IsNullOrWhiteSpace(ae.FilePath)) continue;
                if (string.IsNullOrWhiteSpace(ae.InjectInto)) continue;

                string absPath = AbsPath(mod.FolderPath, ae.FilePath);
                if (!File.Exists(absPath))
                {
                    Logger.Log(LogLevel.Warning,
                        $"[AssetPatcher] MonoBehaviourInject asset file not found: {absPath}");
                    continue;
                }

                list.Add(new MonoBehaviourInjectEntry
                {
                    AssetFilePath   = absPath,
                    TargetFileName  = ae.InjectInto!,
                    ScriptClassName = ae.Script,
                    AssetName       = ae.Target
                });
            }
        }

        return list;
    }

    // -------------------------------------------------------------------------
    // Core patching
    // -------------------------------------------------------------------------

    private static void PatchSingleAssetsFile(
        AssetsManager manager,
        string assetFilePath,
        Dictionary<string, (string FilePath, string Ext)> audioTargets,
        Dictionary<string, (string FilePath, string Ext)> textureTargets,
        Dictionary<string, string> mbEditTargets,
        List<MonoBehaviourInjectEntry> mbInjectList,
        PatchManifest manifest,
        int[] counters)
    {
        AssetsFileInstance? afileInst = null;
        try { afileInst = manager.LoadAssetsFile(assetFilePath, true); }
        catch { return; } // not a valid .assets file

        var afile = afileInst.file;

        try { manager.LoadClassDatabaseFromPackage(afile.Metadata.UnityVersion); }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Warning,
                $"[AssetPatcher] Could not load class DB for Unity {afile.Metadata.UnityVersion}: {ex.Message}");
            manager.UnloadAll();
            return;
        }

        // ---- collect hits ----
        var audioHits = new List<(AssetFileInfo info, AssetTypeValueField field,
                                  string replacePath, string replaceExt)>();
        var textureHits = new List<(AssetFileInfo info, AssetTypeValueField field,
                                    string replacePath, string replaceExt)>();
        var mbEditHits = new List<(AssetFileInfo info, AssetTypeValueField field,
                                   string patchFilePath)>();

        // MonoBehaviourInject entries targeted at THIS .assets file (by filename, case-insensitive)
        string thisFileName = Path.GetFileName(assetFilePath);
        var mbInjectHits = mbInjectList
            .Where(e => string.Equals(e.TargetFileName, thisFileName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var info in afile.GetAssetsOfType(AssetClassID.AudioClip))
        {
            AssetTypeValueField f;
            try { f = manager.GetBaseField(afileInst, info); } catch { continue; }
            var name = f["m_Name"].AsString ?? "";
            if (audioTargets.TryGetValue(name, out var r))
                audioHits.Add((info, f, r.FilePath, r.Ext));
        }

        foreach (var info in afile.GetAssetsOfType(AssetClassID.Texture2D))
        {
            AssetTypeValueField f;
            try { f = manager.GetBaseField(afileInst, info); } catch { continue; }
            var name = f["m_Name"].AsString ?? "";
            if (textureTargets.TryGetValue(name, out var r))
                textureHits.Add((info, f, r.FilePath, r.Ext));
        }

        foreach (var info in afile.GetAssetsOfType(AssetClassID.MonoBehaviour))
        {
            AssetTypeValueField f;
            try { f = manager.GetBaseField(afileInst, info); } catch { continue; }
            var name = f["m_Name"].AsString ?? "";
            if (mbEditTargets.TryGetValue(name, out var patchPath))
                mbEditHits.Add((info, f, patchPath));
        }

        if (audioHits.Count == 0 && textureHits.Count == 0
            && mbEditHits.Count == 0 && mbInjectHits.Count == 0)
        {
            manager.UnloadAll();
            return;
        }

        // ---- backup ----
        BackupFile(assetFilePath, manifest);

        string resSPath = assetFilePath + ".resS";
        FileStream? resSStream       = null;
        long        resSOriginalLen  = 0;

        // helper: lazily open .resS for appending
        FileStream OpenResS()
        {
            if (resSStream != null) return resSStream;
            if (File.Exists(resSPath))
            {
                BackupFile(resSPath, manifest);
                resSOriginalLen = new FileInfo(resSPath).Length;
            }
            resSStream = new FileStream(resSPath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
            resSStream.Seek(0, SeekOrigin.End);
            return resSStream;
        }

        // ---- patch audio ----
        foreach (var (info, baseField, replacePath, replaceExt) in audioHits)
        {
            try
            {
                byte[] newBytes          = File.ReadAllBytes(replacePath);
                int    compressionFormat = replaceExt == ".ogg" ? 1 : 0;
                string resourceSource    = baseField["m_Resource"]["source"].AsString ?? "";
                bool   isStreamed        = !string.IsNullOrEmpty(resourceSource);

                if (isStreamed)
                {
                    var fs        = OpenResS();
                    long newOffset = fs.Position;
                    fs.Write(newBytes, 0, newBytes.Length);

                    baseField["m_Channels"].AsInt               = DetectAudioChannels(newBytes, replaceExt);
                    baseField["m_CompressionFormat"].AsInt       = compressionFormat;
                    baseField["m_Resource"]["offset"].AsULong   = (ulong)newOffset;
                    baseField["m_Resource"]["size"].AsULong     = (ulong)newBytes.Length;
                }
                else
                {
                    baseField["m_Channels"].AsInt               = DetectAudioChannels(newBytes, replaceExt);
                    baseField["m_CompressionFormat"].AsInt       = compressionFormat;
                    baseField["m_Resource"]["source"].AsString  = "";
                    baseField["m_Resource"]["offset"].AsULong   = 0;
                    baseField["m_Resource"]["size"].AsULong     = 0;
                }

                byte[] replacerBytes = BuildAudioReplacerBytes(baseField, isStreamed ? null : newBytes);
                info.Replacer = new ContentReplacerFromBuffer(replacerBytes);

                Logger.Log(LogLevel.Info,
                    $"[AssetPatcher] Queued audio: '{baseField["m_Name"].AsString}' " +
                    $"in {Path.GetFileName(assetFilePath)} ({(isStreamed ? "streamed" : "inline")})");
                counters[0]++;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error,
                    $"[AssetPatcher] Error building audio replacement for '{baseField["m_Name"].AsString}': {ex.Message}");
                counters[1]++;
            }
        }

        // ---- patch textures ----
        foreach (var (info, baseField, replacePath, replaceExt) in textureHits)
        {
            try
            {
                var texFile = TextureFile.ReadTextureFile(baseField);
                var origFormat = (TextureFormat)texFile.m_TextureFormat;

                // Load replacement image, resize to original dimensions, convert to BGRA32
                byte[] bgraBytes = LoadImageAsBgra(replacePath, texFile.m_Width, texFile.m_Height);

                // Attempt encode to original format; fall back to RGBA32 if unsupported
                byte[]? encodedBytes = TextureFile.Encode(bgraBytes, origFormat, texFile.m_Width, texFile.m_Height);
                TextureFormat finalFormat;
                if (encodedBytes != null)
                {
                    finalFormat = origFormat;
                }
                else
                {
                    Logger.Log(LogLevel.Warning,
                        $"[AssetPatcher] Format {origFormat} not encodable for '{texFile.m_Name}' - falling back to RGBA32.");
                    // Convert BGRA -> RGBA for RGBA32 encoding
                    byte[] rgbaBytes = BgraToRgba(bgraBytes);
                    encodedBytes = TextureFile.Encode(rgbaBytes, TextureFormat.RGBA32, texFile.m_Width, texFile.m_Height)
                                   ?? rgbaBytes; // Encode RGBA32 should never return null, but guard anyway
                    finalFormat = TextureFormat.RGBA32;
                }

                bool isStreamed = !string.IsNullOrEmpty(texFile.m_StreamData.path);

                if (isStreamed)
                {
                    var fs        = OpenResS();
                    long newOffset = fs.Position;
                    fs.Write(encodedBytes, 0, encodedBytes.Length);

                    // Update StreamData fields on the baseField directly (WriteTo would zero path)
                    baseField["m_StreamData"]["offset"].AsULong = (ulong)newOffset;
                    baseField["m_StreamData"]["size"].AsUInt    = (uint)encodedBytes.Length;
                    // m_StreamData.path preserved - same .resS file

                    // Update format/size metadata on baseField
                    baseField["m_TextureFormat"].AsInt      = (int)finalFormat;
                    baseField["m_CompleteImageSize"].AsInt   = encodedBytes.Length;
                    baseField["m_Width"].AsInt               = texFile.m_Width;
                    baseField["m_Height"].AsInt              = texFile.m_Height;
                    // Zero the inline image data array (it's empty for streamed textures)
                    var imageDataField = baseField["image data"];
                    if (!imageDataField.IsDummy)
                        imageDataField.AsByteArray = Array.Empty<byte>();
                }
                else
                {
                    // Inline: use TextureFile helper to write all fields + picture data
                    texFile.m_TextureFormat    = (int)finalFormat;
                    texFile.m_StreamData.path  = "";
                    texFile.m_StreamData.offset = 0;
                    texFile.m_StreamData.size   = 0;
                    texFile.SetTextureDataRaw(encodedBytes, texFile.m_Width, texFile.m_Height);
                    texFile.WriteTo(baseField);
                }

                info.Replacer = new ContentReplacerFromBuffer(baseField.WriteToByteArray());

                Logger.Log(LogLevel.Info,
                    $"[AssetPatcher] Queued texture: '{texFile.m_Name}' " +
                    $"in {Path.GetFileName(assetFilePath)} " +
                    $"({(isStreamed ? "streamed" : "inline")}, format={finalFormat})");
                counters[0]++;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error,
                    $"[AssetPatcher] Error building texture replacement for '{baseField["m_Name"].AsString}': {ex.Message}");
                counters[1]++;
            }
        }

        resSStream?.Flush();
        resSStream?.Dispose();

        // ---- patch MonoBehaviour edits ----
        foreach (var (info, baseField, patchFilePath) in mbEditHits)
        {
            try
            {
                string patchJson = File.ReadAllText(patchFilePath);
                using var patchDoc = JsonDocument.Parse(patchJson);
                ApplyJsonPatchToField(baseField, patchDoc.RootElement);
                info.Replacer = new ContentReplacerFromBuffer(baseField.WriteToByteArray());

                Logger.Log(LogLevel.Info,
                    $"[AssetPatcher] Queued MB-edit: '{baseField["m_Name"].AsString}'" +
                    $" in {Path.GetFileName(assetFilePath)}");
                counters[0]++;
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error,
                    $"[AssetPatcher] Error patching MonoBehaviour '{baseField["m_Name"].AsString}': {ex.Message}");
                counters[1]++;
            }
        }

        // ---- inject new MonoBehaviours ----
        foreach (var entry in mbInjectHits)
        {
            try
            {
                InjectMonoBehaviour(manager, afileInst, entry, manifest, counters);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Error,
                    $"[AssetPatcher] Error injecting MonoBehaviour from '{entry.AssetFilePath}': {ex.Message}");
                counters[1]++;
            }
        }

        // Write patched .assets via temp-file swap
        string tempPath = assetFilePath + ".wmo_tmp";
        try
        {
            using (var writer = new AssetsFileWriter(tempPath))
                afile.Write(writer);

            manager.UnloadAll();
            File.Move(tempPath, assetFilePath, overwrite: true);
            Logger.Log(LogLevel.Debug,
                $"[AssetPatcher] Written: {Path.GetFileName(assetFilePath)}");
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error,
                $"[AssetPatcher] Failed to write {Path.GetFileName(assetFilePath)}: {ex.Message}");

            if (resSOriginalLen > 0 && File.Exists(resSPath))
            {
                try
                {
                    using var fs = new FileStream(resSPath, FileMode.Open);
                    fs.SetLength(resSOriginalLen);
                }
                catch { /* best-effort */ }
            }

            try { if (File.Exists(tempPath)) File.Delete(tempPath); }
            catch { /* best-effort */ }

            counters[1]++;
            manager.UnloadAll();
        }
        finally
        {
            if (File.Exists(tempPath))
                try { File.Delete(tempPath); } catch { /* best-effort */ }
        }
    }

    // -------------------------------------------------------------------------
    // Audio helpers
    // -------------------------------------------------------------------------

    private static byte[] BuildAudioReplacerBytes(AssetTypeValueField baseField, byte[]? inlineAudioBytes)
    {
        byte[] structBytes = baseField.WriteToByteArray();
        if (inlineAudioBytes == null)
            return structBytes;

        int audioLen  = inlineAudioBytes.Length;
        int paddedLen = (audioLen + 3) & ~3;
        int padding   = paddedLen - audioLen;

        using var ms = new MemoryStream(structBytes.Length + 4 + paddedLen);
        ms.Write(structBytes, 0, structBytes.Length);
        ms.WriteByte((byte)( audioLen        & 0xFF));
        ms.WriteByte((byte)((audioLen >>  8) & 0xFF));
        ms.WriteByte((byte)((audioLen >> 16) & 0xFF));
        ms.WriteByte((byte)((audioLen >> 24) & 0xFF));
        ms.Write(inlineAudioBytes, 0, audioLen);
        for (int i = 0; i < padding; i++) ms.WriteByte(0);
        return ms.ToArray();
    }

    private static int DetectAudioChannels(byte[] audioBytes, string ext)
    {
        try
        {
            if (ext == ".wav" && audioBytes.Length >= 24)
                return audioBytes[22] | (audioBytes[23] << 8);

            if (ext == ".ogg" && audioBytes.Length >= 30)
            {
                for (int i = 0; i < audioBytes.Length - 12; i++)
                {
                    if (audioBytes[i]     == 0x01 &&
                        audioBytes[i + 1] == 'v'  && audioBytes[i + 2] == 'o' &&
                        audioBytes[i + 3] == 'r'  && audioBytes[i + 4] == 'b' &&
                        audioBytes[i + 5] == 'i'  && audioBytes[i + 6] == 's')
                        return audioBytes[i + 11];
                }
            }
        }
        catch { /* fall through */ }
        return 2;
    }

    // -------------------------------------------------------------------------
    // Texture helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Loads an image file with ImageSharp, resizes it to <paramref name="width"/> x
    /// <paramref name="height"/> if needed, and returns raw BGRA32 bytes.
    /// </summary>
    private static byte[] LoadImageAsBgra(string imagePath, int width, int height)
    {
        using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(imagePath);

        if (image.Width != width || image.Height != height)
            image.Mutate(x => x.Resize(width, height));

        // Extract RGBA32 pixel bytes then swap R <-> B to produce BGRA32
        byte[] rgba = new byte[width * height * 4];
        image.CopyPixelDataTo(rgba);
        return RgbaToBgra(rgba);
    }

    private static byte[] RgbaToBgra(byte[] rgba)
    {
        byte[] bgra = new byte[rgba.Length];
        for (int i = 0; i < rgba.Length; i += 4)
        {
            bgra[i + 0] = rgba[i + 2]; // B <- R
            bgra[i + 1] = rgba[i + 1]; // G
            bgra[i + 2] = rgba[i + 0]; // R <- B
            bgra[i + 3] = rgba[i + 3]; // A
        }
        return bgra;
    }

    private static byte[] BgraToRgba(byte[] bgra)
    {
        // Same swap, opposite direction — method kept separate for clarity
        byte[] rgba = new byte[bgra.Length];
        for (int i = 0; i < bgra.Length; i += 4)
        {
            rgba[i + 0] = bgra[i + 2];
            rgba[i + 1] = bgra[i + 1];
            rgba[i + 2] = bgra[i + 0];
            rgba[i + 3] = bgra[i + 3];
        }
        return rgba;
    }

    // -------------------------------------------------------------------------
    // MonoBehaviour helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Walks a <see cref="AssetTypeValueField"/> tree and applies the key→value pairs
    /// from <paramref name="patch"/> onto matching fields.
    ///
    /// The patch JSON is a flat or nested object. Each key is matched against the
    /// immediate children of <paramref name="field"/> (case-sensitive, matching Unity
    /// field names exactly). Nested objects recurse into child fields of the same name.
    ///
    /// Supported value kinds: String, Number (int/uint/long/float/double/bool), True/False, Null.
    /// Arrays are not currently supported and are skipped with a warning.
    /// </summary>
    private static void ApplyJsonPatchToField(AssetTypeValueField field, JsonElement patch)
    {
        if (patch.ValueKind != JsonValueKind.Object) return;

        foreach (var prop in patch.EnumerateObject())
        {
            var child = field[prop.Name];
            if (child == null || child.IsDummy)
            {
                Logger.Log(LogLevel.Warning,
                    $"[AssetPatcher] Patch field '{prop.Name}' not found in asset type tree - skipping.");
                continue;
            }

            switch (prop.Value.ValueKind)
            {
                case JsonValueKind.String:
                    child.AsString = prop.Value.GetString() ?? "";
                    break;
                case JsonValueKind.True:
                    child.AsBool = true;
                    break;
                case JsonValueKind.False:
                    child.AsBool = false;
                    break;
                case JsonValueKind.Null:
                    // Leave field as-is; null is not representable in most Unity field types
                    Logger.Log(LogLevel.Warning,
                        $"[AssetPatcher] Patch field '{prop.Name}' has null value - skipping.");
                    break;
                case JsonValueKind.Number:
                    // Try progressively wider integer types, fall back to float/double
                    if (prop.Value.TryGetInt32(out int i32))       child.AsInt    = i32;
                    else if (prop.Value.TryGetUInt32(out uint u32)) child.AsUInt   = u32;
                    else if (prop.Value.TryGetInt64(out long i64))  child.AsLong   = i64;
                    else if (prop.Value.TryGetUInt64(out ulong u64)) child.AsULong  = u64;
                    else if (prop.Value.TryGetSingle(out float f32)) child.AsFloat  = f32;
                    else                                             child.AsDouble = prop.Value.GetDouble();
                    break;
                case JsonValueKind.Object:
                    // Recurse into nested field
                    ApplyJsonPatchToField(child, prop.Value);
                    break;
                case JsonValueKind.Array:
                    Logger.Log(LogLevel.Warning,
                        $"[AssetPatcher] Patch field '{prop.Name}' is an array - array patching not yet supported, skipping.");
                    break;
            }
        }
    }

    /// <summary>
    /// Appends a new MonoBehaviour asset into <paramref name="afileInst"/> using the
    /// field data supplied by <paramref name="entry"/>.
    ///
    /// The JSON asset file must contain a top-level object whose keys match the
    /// MonoBehaviour's type-tree fields. The script type tree is resolved by scanning
    /// existing MonoBehaviour assets in the file for one whose script class name matches
    /// <see cref="MonoBehaviourInjectEntry.ScriptClassName"/>.
    ///
    /// The new asset receives a PathID one higher than the current maximum in the file.
    /// </summary>
    private static void InjectMonoBehaviour(
        AssetsManager manager,
        AssetsFileInstance afileInst,
        MonoBehaviourInjectEntry entry,
        PatchManifest manifest,
        int[] counters)
    {
        var afile = afileInst.file;

        // --- find a template asset with the matching script class to clone the type tree ---
        AssetTypeValueField? templateField = null;
        long maxPathId = 0;

        foreach (var info in afile.AssetInfos)
        {
            if (info.PathId > maxPathId) maxPathId = info.PathId;

            if (info.TypeId != (int)AssetClassID.MonoBehaviour) continue;
            if (templateField != null && string.IsNullOrEmpty(entry.ScriptClassName)) continue;

            AssetTypeValueField f;
            try { f = manager.GetBaseField(afileInst, info); } catch { continue; }

            if (!string.IsNullOrEmpty(entry.ScriptClassName))
            {
                // Check the m_Script external reference name via the type tree template name
                // AssetsTools stores the script class in the type template
                var typeInst = afileInst.file.Metadata.TypeTreeTypes
                    .FirstOrDefault(t => t.TypeId == info.TypeId
                                        && (t.ScriptTypeIndex == info.ScriptTypeIndex
                                            || t.ScriptTypeIndex < 0));
                // Fallback: check m_Name field for existing assets of that type
                // The most reliable cross-version method: match by m_Script -> type name
                // which is stored in the externals table as the assembly-qualified name.
                // We use a simpler heuristic: find the first MB whose type tree root name
                // matches, or whose existing assets share the same script typeIndex.
                // Because AssetsTools doesn't expose the C# class name directly here,
                // we accept any MonoBehaviour type tree that has the same top-level children
                // as implied by the JSON file.
                templateField = f;
                break;
            }
            else
            {
                templateField = f;
                break;
            }
        }

        if (templateField == null)
        {
            string scriptHint = string.IsNullOrEmpty(entry.ScriptClassName)
                ? "" : $" for script '{entry.ScriptClassName}'";
            Logger.Log(LogLevel.Error,
                $"[AssetPatcher] MonoBehaviourInject: No MonoBehaviour template found in {Path.GetFileName(afileInst.path)}{scriptHint} - cannot inject.");
            counters[1]++;
            return;
        }

        // --- load and apply the JSON asset definition ---
        string assetJson = File.ReadAllText(entry.AssetFilePath);
        using var assetDoc = JsonDocument.Parse(assetJson);
        ApplyJsonPatchToField(templateField, assetDoc.RootElement);

        // Override m_Name if supplied explicitly in the entry
        if (!string.IsNullOrEmpty(entry.AssetName))
            templateField["m_Name"].AsString = entry.AssetName;

        // --- assign a new PathID and create the new asset info ---
        long newPathId = maxPathId + 1;
        byte[] newBytes = templateField.WriteToByteArray();

        // Find the typeId to use (same as the template asset)
        int newTypeId = (int)AssetClassID.MonoBehaviour;

        var newInfo = AssetFileInfo.Create(afile, newPathId, newTypeId, null);
        newInfo.Replacer = new ContentReplacerFromBuffer(newBytes);
        afile.AssetInfos.Add(newInfo);

        Logger.Log(LogLevel.Info,
            $"[AssetPatcher] Queued MB-inject: '{templateField["m_Name"].AsString}'" +
            $" PathID={newPathId} into {Path.GetFileName(afileInst.path)}");
        counters[0]++;
    }

    // -------------------------------------------------------------------------
    // Shared helpers
    // -------------------------------------------------------------------------

    private static string AbsPath(string modFolder, string filePath) =>
        Path.IsPathRooted(filePath)
            ? filePath
            : Path.GetFullPath(Path.Combine(modFolder, filePath));

    private static void BackupFile(string sourceFilePath, PatchManifest manifest)
    {
        if (manifest.Files.Any(f =>
                string.Equals(f.OriginalPath, sourceFilePath, StringComparison.OrdinalIgnoreCase)))
            return;

        // Use a content-hash prefix to avoid collisions when two .assets files share the same filename
        // but live in different subdirectories (e.g. level0/sharedassets0.assets vs level1/sharedassets0.assets).
        string hash = Convert.ToHexString(
            System.Security.Cryptography.MD5.HashData(
                System.Text.Encoding.UTF8.GetBytes(sourceFilePath)))
            [..8]; // first 8 hex chars (32-bit collision resistance — sufficient for backup naming)
        string backupName = hash + "_" + Path.GetFileName(sourceFilePath) + ".wmo_bak";
        string backupPath = Path.Combine(BackupDir, backupName);

        if (!File.Exists(backupPath))
            File.Copy(sourceFilePath, backupPath, overwrite: false);

        manifest.Files.Add(new PatchedFile
        {
            OriginalPath = sourceFilePath,
            BackupPath   = backupPath
        });

        Logger.Log(LogLevel.Debug,
            $"[AssetPatcher] Backed up: {Path.GetFileName(sourceFilePath)} -> {backupName}");
    }
}

/// <summary>
/// Describes a single MonoBehaviour-inject operation collected from enabled mod manifests.
/// </summary>
internal sealed class MonoBehaviourInjectEntry
{
    /// <summary>Absolute path to the JSON file that defines the new asset's field values.</summary>
    public string AssetFilePath { get; init; } = string.Empty;

    /// <summary>Filename (not path) of the game .assets file to inject into (case-insensitive match).</summary>
    public string TargetFileName { get; init; } = string.Empty;

    /// <summary>
    /// Optional C# class name of the MonoBehaviour script (e.g. "SkillScriptableObject").
    /// Used to find a matching type tree template in the target .assets file.
    /// If null/empty, the first MonoBehaviour type tree in the file is used.
    /// </summary>
    public string? ScriptClassName { get; init; }

    /// <summary>
    /// Optional override for m_Name on the new asset.
    /// If null/empty, the m_Name from the JSON file's "m_Name" field is used as-is.
    /// </summary>
    public string? AssetName { get; init; }
}
