using System.Reflection;
using System.Runtime.Loader;

namespace Mythra.Infrastructure.Addons;

/// <summary>
/// Isolated AssemblyLoadContext for a single addon.
/// isCollectible: true allows the ALC (and the addon's assemblies) to be unloaded
/// when UnloadAddonAsync is called, freeing all associated memory.
///
/// Resolution strategy:
///   1. Use AssemblyDependencyResolver for the addon's own dependencies.
///   2. If unresolved, fall back to the host's default ALC — this covers
///      Mythra.Addons.Contracts and framework assemblies without duplicating them.
/// </summary>
internal sealed class AddonAssemblyContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    public AddonAssemblyContext(string addonId, string entryAssemblyPath)
        : base(addonId, isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(entryAssemblyPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // Never shadow contracts — always share the host's copy so interface equality holds.
        if (assemblyName.Name is "Mythra.Addons.Contracts"
                               or "Microsoft.Extensions.Logging.Abstractions")
            return null;

        var resolved = _resolver.ResolveAssemblyToPath(assemblyName);
        return resolved is not null ? LoadFromAssemblyPath(resolved) : null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var resolved = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        return resolved is not null ? LoadUnmanagedDllFromPath(resolved) : IntPtr.Zero;
    }
}
