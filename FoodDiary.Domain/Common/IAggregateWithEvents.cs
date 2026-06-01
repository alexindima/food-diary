namespace FoodDiary.Domain.Common;

public interface IAggregateWithEvents {
    IReadOnlyList<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}
