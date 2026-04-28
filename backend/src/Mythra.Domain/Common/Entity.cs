namespace Mythra.Domain.Common;

public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; protected set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; protected set; }

    public void Touch() => UpdatedAt = DateTimeOffset.UtcNow;

    public override bool Equals(object? obj) =>
        obj is Entity other && Id == other.Id && GetType() == other.GetType();

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity? a, Entity? b) => a?.Equals(b) ?? b is null;

    public static bool operator !=(Entity? a, Entity? b) => !(a == b);
}
