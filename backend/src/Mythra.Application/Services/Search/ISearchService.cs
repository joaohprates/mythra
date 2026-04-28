using Mythra.Application.Dtos.Search;
using Mythra.Domain.Common;

namespace Mythra.Application.Services.Search;

public interface ISearchService
{
    Task<Result<UnifiedSearchResult>> SearchAsync(UnifiedSearchRequest request, CancellationToken ct = default);
}
