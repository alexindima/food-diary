namespace FoodDiary.MailRelay.Domain.Common;

public interface IAggregateWithEvents {
    IReadOnlyList<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}

public abstract class AggregateRoot<TId> : Entity<TId>, IAggregateWithEvents
    where TId : notnull {
    private readonly List<IDomainEvent> _domainEvents = [];

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
