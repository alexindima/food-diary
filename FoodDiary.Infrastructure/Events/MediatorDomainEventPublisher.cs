using FoodDiary.Application.Abstractions.Common.Abstractions.Events;
using FoodDiary.Domain.Common;
using FoodDiary.Mediator;

namespace FoodDiary.Infrastructure.Events;

internal sealed class MediatorDomainEventPublisher(IPublisher publisher) : IDomainEventPublisher {
    public Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default) {
        return publisher.Publish(new DomainEventNotification(domainEvent), cancellationToken);
    }
}
