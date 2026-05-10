using Mythra.Addons.Contracts;
using Mythra.Addons.Contracts.Models;

namespace Mythra.Application.Abstractions.Addons;

/// <summary>
/// Manages the lifecycle of runtime-loaded addons.
/// Registered as a singleton; the infrastructure IHostedService implementation
/// populates it at startup by scanning the addons directory.
/// </summary>
public interface IAddonHost
{
    // ── Registry ─────────────────────────────────────────────────────────────

    IReadOnlyList<IMetadataAddon> MetadataAddons { get; }
    IReadOnlyList<IStreamSourceAddon> StreamSourceAddons { get; }
    IReadOnlyList<IBookSourceAddon> BookSourceAddons { get; }
    IReadOnlyList<ISubtitleAddon> SubtitleAddons { get; }

    /// <summary>Returns any loaded addon by its manifest ID, or null if not loaded.</summary>
    IAddon? GetById(string addonId);

    // ── Dynamic loading ───────────────────────────────────────────────────────

    /// <summary>
    /// Load an addon from the given directory path (must contain manifest.json).
    /// Returns true if successfully loaded and initialized.
    /// </summary>
    Task<bool> TryLoadAddonAsync(string addonDirectory, CancellationToken ct = default);

    /// <summary>
    /// Gracefully dispose and unload the addon with the given ID.
    /// No-op if the addon is not loaded.
    /// </summary>
    Task UnloadAddonAsync(string addonId, CancellationToken ct = default);

    // ── Status ────────────────────────────────────────────────────────────────

    IReadOnlyList<LoadedAddonInfo> GetLoadedAddons();
}

public sealed record LoadedAddonInfo(
    string Id,
    string Name,
    string Version,
    IReadOnlyList<AddonCapability> Capabilities,
    AddonHealthStatus HealthStatus,
    DateTimeOffset LoadedAt);
