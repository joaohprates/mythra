namespace Mythra.Application.Abstractions.Providers;

/// <summary>
/// Runtime registry for addon-provided <see cref="IExternalVideoProvider"/> instances.
/// Addons that implement <c>IStreamSourceAddon</c> are wrapped by a bridge and
/// registered here at load time, then unregistered when the addon is unloaded.
/// </summary>
public interface IAddonStreamSourceRegistry
{
    void Register(string addonId, IExternalVideoProvider provider);
    void Unregister(string addonId);
    IReadOnlyList<IExternalVideoProvider> GetAll();
}

/// <summary>
/// Runtime registry for addon-provided <see cref="IExternalBookProvider"/> instances.
/// </summary>
public interface IAddonBookSourceRegistry
{
    void Register(string addonId, IExternalBookProvider provider);
    void Unregister(string addonId);
    IReadOnlyList<IExternalBookProvider> GetAll();
}
