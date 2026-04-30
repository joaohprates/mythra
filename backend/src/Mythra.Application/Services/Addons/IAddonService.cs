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

public interface IAddonService
{
    Task<Result<IReadOnlyList<AddonDto>>> ListAsync(Guid userId, CancellationToken ct = default);
    Task<Result<AddonDto>> GetAsync(Guid userId, Guid addonId, CancellationToken ct = default);
    Task<Result<AddonDto>> ImportAsync(Guid userId, string json, CancellationToken ct = default);
    Task<Result<AddonExportDto>> ExportAsync(Guid userId, Guid addonId, CancellationToken ct = default);
    Task<Result<AddonDto>> ToggleAsync(Guid userId, Guid addonId, CancellationToken ct = default);
    Task<Result> DeleteAsync(Guid userId, Guid addonId, CancellationToken ct = default);
}
