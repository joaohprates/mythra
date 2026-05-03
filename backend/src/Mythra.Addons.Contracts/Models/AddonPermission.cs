namespace Mythra.Addons.Contracts.Models;

/// <summary>
/// Permissions an addon may declare in its manifest.
/// The host enforces these at runtime — any call that requires a permission
/// the addon didn't declare results in an AddonPermissionException.
/// </summary>
[Flags]
public enum AddonPermission
{
    None          = 0,

    /// <summary>Make outbound HTTP requests.</summary>
    Network       = 1 << 0,

    /// <summary>Read from and write to the shared addon cache.</summary>
    Cache         = 1 << 1,

    /// <summary>Access the addon's user-configured secrets (API keys, tokens).</summary>
    ReadSecrets   = 1 << 2,

    /// <summary>Access the addon's non-sensitive configuration values.</summary>
    ReadConfig    = 1 << 3,

    /// <summary>Read titles, genres, and IDs from the media library.</summary>
    ReadLibrary   = 1 << 4,

    /// <summary>Write or update metadata fields on media items.</summary>
    WriteMetadata = 1 << 5,
}
