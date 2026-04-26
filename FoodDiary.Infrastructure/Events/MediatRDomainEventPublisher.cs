using FoodDiary.Application.Abstractions.Common.Abstractions.Events;
using FoodDiary.Domain.Common;
using MediatR;

namespace FoodDiary.Infrastructure.Events;

internal sealed class MediatRDomainEventPublisher(IPublisher publisher) : IDomainEventPublisher {
    public Task PublishAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default) {
        return publisher.Publish(new DomainEventNotification(domainEvent), cancellationToken);
    }
}
