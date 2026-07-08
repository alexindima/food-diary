using FoodDiary.Domain.Primitives;

namespace FoodDiary.Application.Abstractions.Common.Abstractions.Events;

public interface IDomainEventPublisher {
    Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
}
