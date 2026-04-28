namespace Mythra.Domain.Common;

public abstract class AggregateRoot : Entity
{
    private readonly List<IDomainEvent> _events = [];

    public IReadOnlyList<IDomainEvent> DomainEvents => _events.AsReadOnly();

    protected void Raise(IDomainEvent @event) => _events.Add(@event);

    public void ClearDomainEvents() => _events.Clear();
}
