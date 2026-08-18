using System.Reflection;
using System.Runtime.InteropServices;
using WMO.Core.Logging;
using WMO.Core.Models.Enums;

namespace WMO.Core.Helpers;

/// <summary>
/// Inspects a BepInEx plugin DLL for the CoreLibrary.AffectsGameplayAttribute
/// without loading the assembly into the main AppDomain.
///
/// Uses MetadataLoadContext so Unity/BepInEx dependencies are never resolved —
/// we only read attribute metadata, never execute any code.
/// </summary>
public static class DllInspector
{
    // Attribute full name as it will appear in the DLL metadata.
    // Must match the namespace + class name in CoreLibrary exactly.
    private const string AttributeFullName = "CoreLibrary.AffectsGameplayAttribute";

    /// <summary>
    /// Reads the AffectsGameplayAttribute from the assembly-level custom attributes
    /// of the DLL at <paramref name="dllPath"/>.
    ///
    /// Returns:
    ///   true  — attribute present with Value = true, OR attribute absent (safe default)
    ///   false — attribute present with Value = false (mod author explicitly opted out)
    /// </summary>
    public static bool AffectsGameplay(string dllPath)
    {
        if (!File.Exists(dllPath))
        {
            Logger.Log(LogLevel.Warning, $"[DllInspector] File not found: {dllPath}");
            return true; // safe default
        }

        try
        {
            // Build a resolver that supplies just the runtime core libs.
            // We deliberately do NOT add game or BepInEx DLLs — if a type
            // cannot be resolved, MetadataLoadContext simply skips it, which
            // is fine because we only care about the attribute's constructor
            // argument (a plain bool), not any game type.
            var resolver = new PathAssemblyResolver(GetRuntimeAssemblies());

            using var mlc = new MetadataLoadContext(resolver);

            Assembly asm;
            try
            {
                asm = mlc.LoadFromAssemblyPath(dllPath);
            }
            catch (Exception ex)
            {
                Logger.Log(LogLevel.Warning,
                    $"[DllInspector] Could not load '{Path.GetFileName(dllPath)}': {ex.Message}. Defaulting to AffectsGameplay=true.");
                return true;
            }

            foreach (var attrData in asm.GetCustomAttributesData())
            {
                if (attrData.AttributeType.FullName != AttributeFullName)
                    continue;

                // Constructor argument: AffectsGameplayAttribute(bool value)
                if (attrData.ConstructorArguments.Count == 1 &&
                    attrData.ConstructorArguments[0].Value is bool value)
                {
                    Logger.Log(LogLevel.Debug,
                        $"[DllInspector] '{Path.GetFileName(dllPath)}' → AffectsGameplay={value}");
                    return value;
                }

                // Attribute present but no argument — parameterless ctor defaults to true
                Logger.Log(LogLevel.Debug,
                    $"[DllInspector] '{Path.GetFileName(dllPath)}' → AffectsGameplay=true (no arg)");
                return true;
            }

            // Attribute absent — default to true (unknown DLL is assumed to affect gameplay)
            Logger.Log(LogLevel.Debug,
                $"[DllInspector] '{Path.GetFileName(dllPath)}' → AffectsGameplay=true (attribute absent)");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Warning,
                $"[DllInspector] Unexpected error inspecting '{Path.GetFileName(dllPath)}': {ex.Message}. Defaulting to AffectsGameplay=true.");
            return true;
        }
    }

    /// <summary>
    /// Returns the set of runtime assemblies to seed the MetadataLoadContext resolver.
    /// This gives MetadataLoadContext enough type info to parse basic BCL types
    /// (bool, string, Attribute, etc.) without loading any game-specific DLLs.
    /// </summary>
    private static IEnumerable<string> GetRuntimeAssemblies()
    {
        // RuntimeEnvironment.GetRuntimeDirectory() points to the .NET runtime directory
        // which contains System.Private.CoreLib, System.Runtime, etc.
        string runtimeDir = RuntimeEnvironment.GetRuntimeDirectory();
        return Directory.GetFiles(runtimeDir, "*.dll");
    }
}
