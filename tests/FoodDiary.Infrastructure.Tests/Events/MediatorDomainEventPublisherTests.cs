using FoodDiary.Domain.Events;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Mediator;

namespace FoodDiary.Infrastructure.Tests.Events;

[ExcludeFromCodeCoverage]
public sealed class MediatorDomainEventPublisherTests {
    [Fact]
    public async Task PublishAsync_WrapsConcreteDomainEventTypeInNotificationEnvelope() {
        var publisher = new RecordingPublisher();
        var sut = CreatePublisher(publisher);
        var domainEvent = new RecommendationCreatedDomainEvent(
            RecommendationId.New(),
            UserId.New(),
            UserId.New());

        await sut.PublishAsync(domainEvent, CancellationToken.None);

        Assert.NotNull(publisher.PublishedNotification);
        var notificationType = publisher.PublishedNotification.GetType();
        Assert.True(notificationType.IsGenericType);
        Assert.Equal(typeof(NotificationEnvelope<>), notificationType.GetGenericTypeDefinition());
        Assert.Equal(typeof(RecommendationCreatedDomainEvent), notificationType.GetGenericArguments()[0]);
    }

    private static FoodDiary.Application.Abstractions.Common.Abstractions.Events.IDomainEventPublisher CreatePublisher(
        IPublisher publisher) {
        var type = Type.GetType("FoodDiary.Infrastructure.Events.MediatorDomainEventPublisher, FoodDiary.Infrastructure", throwOnError: true)!;
        var constructor = type.GetConstructors(
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic)
            .Single();
        return (FoodDiary.Application.Abstractions.Common.Abstractions.Events.IDomainEventPublisher)constructor.Invoke([publisher]);
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingPublisher : IPublisher {
        public object? PublishedNotification { get; private set; }

        public Task Publish(object notification, CancellationToken cancellationToken = default) {
            PublishedNotification = notification;
            return Task.CompletedTask;
        }

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification {
            PublishedNotification = notification;
            return Task.CompletedTask;
        }
    }
}
