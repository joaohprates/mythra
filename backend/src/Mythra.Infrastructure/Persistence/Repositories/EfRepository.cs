using Microsoft.EntityFrameworkCore;
using Mythra.Application.Abstractions.Persistence;
using Mythra.Domain.Common;

namespace Mythra.Infrastructure.Persistence.Repositories;

public abstract class EfRepository<T>(MythraDbContext db) : IRepository<T> where T : Entity
{
    protected MythraDbContext Db { get; } = db;
    protected DbSet<T> Set => Db.Set<T>();

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await Set.FindAsync([id], ct);

    public virtual async Task AddAsync(T entity, CancellationToken ct = default)
        => await Set.AddAsync(entity, ct);

    public virtual void Remove(T entity) => Set.Remove(entity);

    public virtual void Update(T entity) => Set.Update(entity);
}
