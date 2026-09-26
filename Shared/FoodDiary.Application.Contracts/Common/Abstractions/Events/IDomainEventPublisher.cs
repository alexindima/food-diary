using FoodDiary.Domain.Primitives;

namespace FoodDiary.Application.Contracts.Common.Abstractions.Events;

public interface IDomainEventPublisher {
    Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
}
