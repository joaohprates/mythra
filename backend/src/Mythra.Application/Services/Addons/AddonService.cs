using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Mythra.Application.Abstractions.Persistence;
using Mythra.Domain.Addons;
using Mythra.Domain.Common;

namespace Mythra.Application.Services.Addons;

public sealed class AddonService(IAddonRepository addons, IUnitOfWork uow) : IAddonService
{
    public async Task<Result<IReadOnlyList<AddonDto>>> ListAsync(Guid userId, CancellationToken ct = default)
    {
        var list = await addons.ListByUserAsync(userId, ct);
        return Result<IReadOnlyList<AddonDto>>.Success(list.Select(ToDto).ToList());
    }

    public async Task<Result<AddonDto>> GetAsync(Guid userId, Guid addonId, CancellationToken ct = default)
    {
        var addon = await addons.GetByIdAsync(addonId, ct);
        if (addon is null || addon.UserId != userId) return Error.NotFound(nameof(Addon), addonId);
        return ToDto(addon);
    }

    public async Task<Result<AddonDto>> ImportAsync(Guid userId, string json, CancellationToken ct = default)
    {
        AddonExportDto? dto;
        try { dto = JsonSerializer.Deserialize<AddonExportDto>(json, JsonOptions); }
        catch { return Error.Validation("Invalid addon JSON."); }

        if (dto is null) return Error.Validation("Addon payload is empty.");
        if (string.IsNullOrWhiteSpace(dto.Name)) return Error.Validation("Addon name is required.");
        if (string.IsNullOrWhiteSpace(dto.ProviderType)) return Error.Validation("ProviderType is required.");

        var checksum = ComputeChecksum(dto.ProviderConfigJson);

        var addon = new Addon(userId, dto.Name, dto.Kind, dto.TargetMediaKind, dto.ProviderType)
        {
            Description = dto.Description,
            IconUrl = dto.IconUrl,
            ProviderConfigJson = dto.ProviderConfigJson,
            SourceChecksum = checksum,
            Status = AddonStatus.PendingSecrets,
        };

        await addons.AddAsync(addon, ct);
        await uow.SaveChangesAsync(ct);
        return ToDto(addon);
    }

    public async Task<Result<AddonExportDto>> ExportAsync(Guid userId, Guid addonId, CancellationToken ct = default)
    {
        var addon = await addons.GetByIdAsync(addonId, ct);
        if (addon is null || addon.UserId != userId) return Error.NotFound(nameof(Addon), addonId);

        return new AddonExportDto(
            addon.Name,
            addon.Description,
            addon.IconUrl,
            addon.Kind,
            addon.TargetMediaKind,
            addon.ProviderType,
            addon.ProviderConfigJson,
            addon.SourceChecksum);
    }

    public async Task<Result<AddonDto>> ToggleAsync(Guid userId, Guid addonId, CancellationToken ct = default)
    {
        var addon = await addons.GetByIdAsync(addonId, ct);
        if (addon is null || addon.UserId != userId) return Error.NotFound(nameof(Addon), addonId);
        addon.Toggle();
        await uow.SaveChangesAsync(ct);
        return ToDto(addon);
    }

    public async Task<Result> DeleteAsync(Guid userId, Guid addonId, CancellationToken ct = default)
    {
        var addon = await addons.GetByIdAsync(addonId, ct);
        if (addon is null || addon.UserId != userId) return Error.NotFound(nameof(Addon), addonId);
        addons.Remove(addon);
        await uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    private static AddonDto ToDto(Addon a) => new(
        a.Id, a.Name, a.Description, a.IconUrl, a.Kind,
        a.TargetMediaKind, a.ProviderType, a.Status, a.ImportedFrom,
        a.CreatedAt, a.UpdatedAt ?? a.CreatedAt);

    private static string ComputeChecksum(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
}
