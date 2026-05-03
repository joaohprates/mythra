using Mythra.Domain.Addons;

namespace Mythra.Application.Abstractions.Addons;

/// <summary>
/// Bridges the persisted Addon entity to a live provider registration.
///
/// When an addon is configured with secrets and activated (toggle or configure),
/// the activator creates the corresponding provider instance and registers it in the
/// appropriate registry (e.g. IMetadataProviderRegistry).
///
/// Implemented in Infrastructure; registered as singleton.
/// </summary>
public interface IAddonActivator
{
    /// <summary>Returns true if this activator can handle the given ProviderType string.</summary>
    bool CanHandle(string providerType);

    /// <summary>
    /// Creates and registers the provider.
    /// No-op if secrets are incomplete or the type is not recognised.
    /// </summary>
    void Activate(Addon addon);

    /// <summary>
    /// Removes the provider from the registry.
    /// No-op if it was never activated.
    /// </summary>
    void Deactivate(Addon addon);
}
