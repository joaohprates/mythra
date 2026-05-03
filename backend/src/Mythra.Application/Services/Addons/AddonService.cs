using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Mythra.Application.Abstractions.Addons;
using Mythra.Application.Abstractions.Persistence;
using Mythra.Domain.Addons;
using Mythra.Domain.Common;

namespace Mythra.Application.Services.Addons;

public sealed class AddonService(
    IAddonRepository addons,
    IAddonActivator activator,
    IUnitOfWork uow) : IAddonService
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
            Description        = dto.Description,
            IconUrl            = dto.IconUrl,
            ProviderConfigJson = dto.ProviderConfigJson,
            SourceChecksum     = checksum,
            Status             = AddonStatus.PendingSecrets,
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

        if (addon.Status == AddonStatus.PendingSecrets)
            return Error.Validation("Configure the addon secrets first before enabling it.");

        var wasActive = addon.Status == AddonStatus.Active;
        addon.Toggle();
        await uow.SaveChangesAsync(ct);

        // Reflect the change in live registries.
        if (wasActive)
            activator.Deactivate(addon);
        else
            activator.Activate(addon);

        return ToDto(addon);
    }

    public async Task<Result> DeleteAsync(Guid userId, Guid addonId, CancellationToken ct = default)
    {
        var addon = await addons.GetByIdAsync(addonId, ct);
        if (addon is null || addon.UserId != userId) return Error.NotFound(nameof(Addon), addonId);

        activator.Deactivate(addon);
        addons.Remove(addon);
        await uow.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<AddonDto>> ConfigureAsync(
        Guid userId, Guid addonId, ConfigureAddonRequest req, CancellationToken ct = default)
    {
        var addon = await addons.GetByIdAsync(addonId, ct);
        if (addon is null || addon.UserId != userId) return Error.NotFound(nameof(Addon), addonId);

        // Merge incoming secrets (never replace entirely — user may configure keys one at a time).
        if (req.Secrets is { Count: > 0 })
        {
            var existing = DeserializeDict(addon.SecretsJson);
            foreach (var (k, v) in req.Secrets) existing[k] = v;
            addon.SecretsJson = JsonSerializer.Serialize(existing, JsonOptions);
        }

        // Merge config values (non-sensitive).
        if (req.Config is { Count: > 0 })
        {
            var existing = DeserializeDict(addon.ProviderConfigJson);
            foreach (var (k, v) in req.Config) existing[k] = v;
            addon.ProviderConfigJson = JsonSerializer.Serialize(existing, JsonOptions);
            addon.SourceChecksum     = ComputeChecksum(addon.ProviderConfigJson);
        }

        // If the addon was pending secrets, try to activate it now.
        if (addon.Status == AddonStatus.PendingSecrets && activator.CanHandle(addon.ProviderType))
        {
            // The activator will log a warning if secrets are still incomplete.
            addon.Status = AddonStatus.Active;
            activator.Activate(addon);
        }
        else if (addon.Status == AddonStatus.Active)
        {
            // Re-activate with updated config/secrets (creates new provider instance).
            activator.Deactivate(addon);
            activator.Activate(addon);
        }

        addon.Touch();
        await uow.SaveChangesAsync(ct);
        return ToDto(addon);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static AddonDto ToDto(Addon a) => new(
        a.Id, a.Name, a.Description, a.IconUrl, a.Kind,
        a.TargetMediaKind, a.ProviderType, a.Status, a.ImportedFrom,
        a.CreatedAt, a.UpdatedAt ?? a.CreatedAt);

    private static string ComputeChecksum(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static Dictionary<string, string> DeserializeDict(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions) ?? []; }
        catch { return []; }
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
}
