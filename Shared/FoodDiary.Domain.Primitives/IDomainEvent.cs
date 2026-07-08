namespace FoodDiary.Domain.Primitives;

/// <summary>
/// Represents a fact raised by the domain model while committing the current transaction.
/// </summary>
/// <remarks>
/// Domain event handlers may create transactional state, including outbox records. They must not call
/// external transports directly; durable cross-process delivery belongs to integration events and outbox messages.
/// </remarks>
public interface IDomainEvent {
    DateTime OccurredOnUtc { get; }

    string EventType => GetType().Name;
}
