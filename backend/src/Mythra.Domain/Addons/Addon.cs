using Mythra.Domain.Common;

namespace Mythra.Domain.Addons;

public enum AddonKind { MetadataProvider = 0, StreamSource = 1, SubtitleProvider = 2, BookSource = 3 }

public enum AddonStatus { PendingSecrets = 0, Active = 1, Disabled = 2 }

public sealed class Addon : Entity
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconUrl { get; set; }
    public AddonKind Kind { get; set; }
    public string TargetMediaKind { get; set; } = string.Empty;
    public string ProviderType { get; set; } = string.Empty;
    public string ProviderConfigJson { get; set; } = "{}";
    public string? SecretsJson { get; set; }
    public AddonStatus Status { get; set; } = AddonStatus.PendingSecrets;
    public string SourceChecksum { get; set; } = string.Empty;
    public string? ImportedFrom { get; set; }

    private Addon() { }

    public Addon(Guid userId, string name, AddonKind kind, string targetMediaKind, string providerType)
    {
        UserId = userId;
        Name = name.Trim();
        Kind = kind;
        TargetMediaKind = targetMediaKind;
        ProviderType = providerType;
    }

    public void Toggle()
    {
        Status = Status == AddonStatus.Active ? AddonStatus.Disabled : AddonStatus.Active;
        Touch();
    }
}
