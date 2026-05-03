using Mythra.Domain.Addons;
using Mythra.Domain.Common;

namespace Mythra.Application.Services.Addons;

public sealed record AddonDto(
    Guid Id,
    string Name,
    string? Description,
    string? IconUrl,
    AddonKind Kind,
    string TargetMediaKind,
    string ProviderType,
    AddonStatus Status,
    string? ImportedFrom,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record AddonExportDto(
    string Name,
    string? Description,
    string? IconUrl,
    AddonKind Kind,
    string TargetMediaKind,
    string ProviderType,
    string ProviderConfigJson,
    string SourceChecksum);

/// <summary>
/// Request body for PATCH /api/v1/addons/{id}/configure.
/// Secrets are stored encrypted (SecretsJson) and never exported.
/// Config is non-sensitive and included in .mythra-addon.json exports.
/// </summary>
public sealed record ConfigureAddonRequest(
    /// <summary>Sensitive keys such as "ApiKey". Merged into the addon's SecretsJson.</summary>
    Dictionary<string, string>? Secrets = null,
    /// <summary>Non-sensitive config such as cache TTLs. Merged into ProviderConfigJson.</summary>
    Dictionary<string, string>? Config = null);

public interface IAddonService
{
    Task<Result<IReadOnlyList<AddonDto>>> ListAsync(Guid userId, CancellationToken ct = default);
    Task<Result<AddonDto>> GetAsync(Guid userId, Guid addonId, CancellationToken ct = default);
    Task<Result<AddonDto>> ImportAsync(Guid userId, string json, CancellationToken ct = default);
    Task<Result<AddonExportDto>> ExportAsync(Guid userId, Guid addonId, CancellationToken ct = default);
    Task<Result<AddonDto>> ToggleAsync(Guid userId, Guid addonId, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid userId, Guid addonId, CancellationToken ct = default);

    /// <summary>
    /// Saves secrets and/or config values. If all required secrets become available
    /// (determined by the activator) the addon is moved to Active and the provider
    /// is registered immediately.
    /// </summary>
    Task<Result<AddonDto>> ConfigureAsync(Guid userId, Guid addonId, ConfigureAddonRequest req, CancellationToken ct = default);
}
