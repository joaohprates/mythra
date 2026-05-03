using System.Text.Json.Serialization;

namespace Mythra.Addons.Contracts.Models;

/// <summary>
/// Describes an addon — read from manifest.json at load time.
/// This is the addon's "contract with the host".
/// </summary>
public sealed class AddonManifest
{
    /// <summary>Globally unique addon identifier (reverse-domain, e.g. "io.mythra.omdb-metadata").</summary>
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }

    /// <summary>SemVer string, e.g. "1.2.0".</summary>
    public string Version { get; init; } = "1.0.0";

    public string? Author { get; init; }
    public string? Homepage { get; init; }

    /// <summary>Minimum Mythra host version required (SemVer).</summary>
    public string MinHostVersion { get; init; } = "0.1.0";

    public List<AddonCapability> Capabilities { get; init; } = [];

    /// <summary>Permissions the addon needs — host will reject if not granted by user.</summary>
    public List<AddonPermission> RequiredPermissions { get; init; } = [];

    /// <summary>Permissions the addon uses when available but can function without.</summary>
    public List<AddonPermission> OptionalPermissions { get; init; } = [];

    public AddonEntryPoint EntryPoint { get; init; } = new();

    /// <summary>Secret keys that must be configured before the addon becomes Active.</summary>
    public List<string> RequiredSecrets { get; init; } = [];

    /// <summary>Optional configuration fields shown in the UI.</summary>
    public List<AddonConfigField> ConfigSchema { get; init; } = [];
}

public sealed class AddonEntryPoint
{
    /// <summary>DLL file name relative to the addon directory (e.g. "MyAddon.dll").</summary>
    public string Assembly { get; init; } = string.Empty;

    /// <summary>Fully-qualified type name of the class implementing IAddon.</summary>
    public string Type { get; init; } = string.Empty;
}

public sealed class AddonConfigField
{
    public string Key { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string? Description { get; init; }
    public AddonConfigFieldType Type { get; init; } = AddonConfigFieldType.String;
    public string? DefaultValue { get; init; }
    public List<string>? SelectOptions { get; init; }
    public bool Required { get; init; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AddonConfigFieldType { String, Number, Boolean, Select }
