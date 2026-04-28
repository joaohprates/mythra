using Mythra.Domain.Common;

namespace Mythra.Application.Abstractions.Persistence;

public interface IRepository<T> where T : Entity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    void Remove(T entity);
    void Update(T entity);
}
