using Mythra.Application.Abstractions.Metadata;
using Mythra.Domain.Media;

namespace Mythra.Infrastructure.Metadata;

public sealed class MetadataProviderRegistry(IEnumerable<IMetadataProvider> providers) : IMetadataProviderRegistry
{
    private readonly List<IMetadataProvider> _providers = providers.ToList();

    public IMetadataProvider? GetByName(string name) =>
        _providers.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));

    public IReadOnlyList<IMetadataProvider> ProvidersFor(MediaKind kind) =>
        _providers.Where(p => p.Supports(kind)).ToList();
}
