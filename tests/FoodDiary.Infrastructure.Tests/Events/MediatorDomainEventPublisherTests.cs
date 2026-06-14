using System.Reflection;
using FoodDiary.Application.Abstractions.Common.Abstractions.Events;
using FoodDiary.Domain.Events;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Mediator;

namespace FoodDiary.Infrastructure.Tests.Events;

[ExcludeFromCodeCoverage]
public sealed class MediatorDomainEventPublisherTests {
    [Fact]
    public async Task PublishAsync_WrapsConcreteDomainEventTypeInNotificationEnvelope() {
        IPublisher publisher = Substitute.For<IPublisher>();
        object? publishedNotification = null;
        publisher
            .Publish(Arg.Do<object>(notification => publishedNotification = notification), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        IDomainEventPublisher sut = CreatePublisher(publisher);
        var domainEvent = new RecommendationCreatedDomainEvent(
            RecommendationId.New(),
            UserId.New(),
            UserId.New());

        await sut.PublishAsync(domainEvent, CancellationToken.None);

        Assert.NotNull(publishedNotification);
        Type notificationType = publishedNotification.GetType();
        Assert.True(notificationType.IsGenericType);
        Assert.Equal(typeof(NotificationEnvelope<>), notificationType.GetGenericTypeDefinition());
        Assert.Equal(typeof(RecommendationCreatedDomainEvent), notificationType.GetGenericArguments()[0]);
    }

    private static FoodDiary.Application.Abstractions.Common.Abstractions.Events.IDomainEventPublisher CreatePublisher(
        IPublisher publisher) {
        Type type = Type.GetType("FoodDiary.Infrastructure.Events.MediatorDomainEventPublisher, FoodDiary.Infrastructure", throwOnError: true)!;
        ConstructorInfo constructor = type.GetConstructors(
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic)
            .Single();
        return (FoodDiary.Application.Abstractions.Common.Abstractions.Events.IDomainEventPublisher)constructor.Invoke([publisher]);
    }
}
