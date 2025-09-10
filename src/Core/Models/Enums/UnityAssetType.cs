using AssetsTools.NET.Extra;

namespace WMO.Core.Models.Enums;

/// <summary>
/// Common Unity asset class types that can be scanned and filtered
/// Based on UABEA ClassTypes and AssetsTools.NET AssetClassID
/// </summary>
public enum UnityAssetType
{
    // Basic asset types
    [AssetTypeInfo("GameObject", AssetClassID.GameObject)]
    GameObject = 1,
    
    [AssetTypeInfo("Component", AssetClassID.Component)]
    Component = 2,
    
    [AssetTypeInfo("LevelGameManager", AssetClassID.LevelGameManager)]
    LevelGameManager = 3,
    
    [AssetTypeInfo("Transform", AssetClassID.Transform)]
    Transform = 4,
    
    [AssetTypeInfo("TimeManager", AssetClassID.TimeManager)]
    TimeManager = 5,
    
    [AssetTypeInfo("GlobalGameManager", AssetClassID.GlobalGameManager)]
    GlobalGameManager = 6,
    
    // Common asset types
    [AssetTypeInfo("Texture2D", AssetClassID.Texture2D)]
    Texture2D = 28,
    
    [AssetTypeInfo("Mesh", AssetClassID.Mesh)]
    Mesh = 43,
    
    [AssetTypeInfo("MeshRenderer", AssetClassID.MeshRenderer)]
    MeshRenderer = 23,
    
    [AssetTypeInfo("Material", AssetClassID.Material)]
    Material = 21,
    
    [AssetTypeInfo("Shader", AssetClassID.Shader)]
    Shader = 48,
    
    [AssetTypeInfo("AudioClip", AssetClassID.AudioClip)]
    AudioClip = 83,
    
    [AssetTypeInfo("AudioSource", AssetClassID.AudioSource)]
    AudioSource = 82,
    
    [AssetTypeInfo("Camera", AssetClassID.Camera)]
    Camera = 20,
    
    [AssetTypeInfo("Light", AssetClassID.Light)]
    Light = 108,
    
    [AssetTypeInfo("Animation", AssetClassID.Animation)]
    Animation = 111,
    
    [AssetTypeInfo("AnimationClip", AssetClassID.AnimationClip)]
    AnimationClip = 74,
    
    [AssetTypeInfo("Animator", AssetClassID.Animator)]
    Animator = 95,
    
    [AssetTypeInfo("AnimatorController", AssetClassID.AnimatorController)]
    AnimatorController = 91,
    
    [AssetTypeInfo("Sprite", AssetClassID.Sprite)]
    Sprite = 212,
    
    [AssetTypeInfo("SpriteRenderer", AssetClassID.SpriteRenderer)]
    SpriteRenderer = 114,
    
    [AssetTypeInfo("Canvas", AssetClassID.Canvas)]
    Canvas = 223,
    
    [AssetTypeInfo("CanvasRenderer", AssetClassID.CanvasRenderer)]
    CanvasRenderer = 222,
    
    // Physics
    [AssetTypeInfo("Rigidbody", AssetClassID.Rigidbody)]
    Rigidbody = 54,
    
    [AssetTypeInfo("Collider", AssetClassID.Collider)]
    Collider = 56,
    
    [AssetTypeInfo("BoxCollider", AssetClassID.BoxCollider)]
    BoxCollider = 65,
    
    [AssetTypeInfo("CapsuleCollider", AssetClassID.CapsuleCollider)]
    CapsuleCollider = 136,
    
    [AssetTypeInfo("MeshCollider", AssetClassID.MeshCollider)]
    MeshCollider = 64,
    
    [AssetTypeInfo("SphereCollider", AssetClassID.SphereCollider)]
    SphereCollider = 135,
    
    // Scripting
    [AssetTypeInfo("MonoScript", AssetClassID.MonoScript)]
    MonoScript = 115,
    
    [AssetTypeInfo("MonoBehaviour", AssetClassID.MonoBehaviour)]
    MonoBehaviour = 114,
    
    [AssetTypeInfo("TextAsset", AssetClassID.TextAsset)]
    TextAsset = 49,
    
    // UI Elements
    [AssetTypeInfo("RectTransform", AssetClassID.RectTransform)]
    RectTransform = 224,
    
    // Note: UI components like Button, Image, Text are typically MonoBehaviour scripts
    // and should be detected as MonoBehaviour type
    
    // Terrain and Environment
    [AssetTypeInfo("Terrain", AssetClassID.Terrain)]
    Terrain = 218,
    
    [AssetTypeInfo("TerrainData", AssetClassID.TerrainData)]
    TerrainData = 156,
    
    // Particle Systems
    [AssetTypeInfo("ParticleSystem", AssetClassID.ParticleSystem)]
    ParticleSystem = 198,
    
    [AssetTypeInfo("ParticleSystemRenderer", AssetClassID.ParticleSystemRenderer)]
    ParticleSystemRenderer = 199,
    
    // Networking and Misc
    [AssetTypeInfo("NetworkView", AssetClassID.NetworkView)]
    NetworkView = 147,
    
    [AssetTypeInfo("Font", AssetClassID.Font)]
    Font = 128,
    
    [AssetTypeInfo("Cubemap", AssetClassID.Cubemap)]
    Cubemap = 89,
    
    [AssetTypeInfo("FlareLayer", AssetClassID.FlareLayer)]
    FlareLayer = 119,
    
    [AssetTypeInfo("Flare", AssetClassID.Flare)]
    Flare = 121,
    
    [AssetTypeInfo("LightmapSettings", AssetClassID.LightmapSettings)]
    LightmapSettings = 157,
    
    [AssetTypeInfo("Skybox", AssetClassID.Skybox)]
    Skybox = 108
}

/// <summary>
/// Attribute to provide asset type information
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class AssetTypeInfoAttribute : Attribute
{
    public string DisplayName { get; }
    public AssetClassID ClassId { get; }
    
    public AssetTypeInfoAttribute(string displayName, AssetClassID classId)
    {
        DisplayName = displayName;
        ClassId = classId;
    }
}

/// <summary>
/// Extension methods for UnityAssetType enum
/// </summary>
public static class UnityAssetTypeExtensions
{
    /// <summary>
    /// Get the display name for an asset type
    /// </summary>
    public static string GetDisplayName(this UnityAssetType assetType)
    {
        var field = assetType.GetType().GetField(assetType.ToString());
        var attribute = field?.GetCustomAttributes(typeof(AssetTypeInfoAttribute), false)
            .FirstOrDefault() as AssetTypeInfoAttribute;
        return attribute?.DisplayName ?? assetType.ToString();
    }
    
    /// <summary>
    /// Get the AssetClassID for an asset type
    /// </summary>
    public static AssetClassID GetClassId(this UnityAssetType assetType)
    {
        var field = assetType.GetType().GetField(assetType.ToString());
        var attribute = field?.GetCustomAttributes(typeof(AssetTypeInfoAttribute), false)
            .FirstOrDefault() as AssetTypeInfoAttribute;
        return attribute?.ClassId ?? AssetClassID.Object;
    }
    
    /// <summary>
    /// Get all asset types that are commonly moddable
    /// </summary>
    public static UnityAssetType[] GetModdableTypes()
    {
        return new[]
        {
            UnityAssetType.Texture2D,
            UnityAssetType.Sprite,
            UnityAssetType.AudioClip,
            UnityAssetType.Material,
            UnityAssetType.Mesh,
            UnityAssetType.AnimationClip,
            UnityAssetType.TextAsset,
            UnityAssetType.Font
        };
    }
}
