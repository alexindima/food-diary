namespace FoodDiary.Application.Abstractions.Common.Abstractions.Events;

/// <summary>
/// Represents a committed application fact intended for another process, service, or provider workflow.
/// </summary>
/// <remarks>
/// Integration events are not domain events. They should be written to durable outbox state inside the
/// same transaction that commits the source change, then delivered asynchronously by an outbox processor.
/// </remarks>
public interface IIntegrationEvent {
    DateTime OccurredOnUtc { get; }
}
