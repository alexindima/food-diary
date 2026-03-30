using FoodDiary.Domain.Common;

namespace FoodDiary.Application.Common.Abstractions.Events;

public interface IDomainEventPublisher {
    Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
}
