namespace WMO.Core.Models.Enums;

/// <summary>
/// Describes how an enabled mod affects online features such as leaderboard submission.
/// Derived by the ModLoader from the AffectsGameplayAttribute embedded in each plugin DLL.
/// </summary>
public enum ModOnlineStatus
{
    /// <summary>
    /// No BepInEx plugin DLL present in this mod — online status is not applicable
    /// (audio/texture-only mods never affect gameplay).
    /// </summary>
    NotApplicable,

    /// <summary>
    /// Every plugin DLL in this mod carries [AffectsGameplay(false)] — online features
    /// such as leaderboard submission remain enabled.
    /// </summary>
    Approved,

    /// <summary>
    /// At least one plugin DLL carries [AffectsGameplay(true)] or has no attribute
    /// (defaults to true) — online features will be disabled for this session.
    /// </summary>
    Disabled,
}
