namespace FoodDiary.Domain.Primitives;

/// <summary>
/// Represents a fact raised by the domain model while committing the current transaction.
/// </summary>
/// <remarks>
/// Domain event handlers may create transactional state, including outbox records. They must not call
/// external transports directly; durable cross-process delivery belongs to integration events and outbox messages.
/// </remarks>
public interface IDomainEvent {
    /// <summary>
    /// Gets the UTC timestamp at which the domain fact occurred.
    /// </summary>
    DateTime OccurredOnUtc { get; }

    /// <summary>
    /// Gets the stable event name used for diagnostics.
    /// </summary>
    /// <remarks>
    /// Override this member when the diagnostic name must remain independent from the CLR type name.
    /// </remarks>
    string EventType => GetType().Name;
}
