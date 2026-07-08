namespace FoodDiary.Domain.Primitives;

public interface IAggregateWithEvents {
    IReadOnlyList<IDomainEvent> DomainEvents { get; }

    void ClearDomainEvents();
}
