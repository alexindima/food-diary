namespace FoodDiary.Domain.Common;

public abstract class AggregateRoot<TId> : Entity<TId>
    where TId : notnull {
    private readonly List<IDomainEvent> _domainEvents = new();

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected AggregateRoot() {
    }

    protected AggregateRoot(TId id) : base(id) {
    }

    protected void RaiseDomainEvent(IDomainEvent domainEvent) {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents() {
        _domainEvents.Clear();
    }
}
