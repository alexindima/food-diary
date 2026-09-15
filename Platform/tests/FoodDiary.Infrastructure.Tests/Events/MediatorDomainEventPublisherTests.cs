using FoodDiary.Modules.Dietologist.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Dietologist.Domain.Events;
using FoodDiary.Persistence.Runtime.Events;
using FoodDiary.Application.Abstractions.Common.Abstractions.Events;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
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
        IDomainEventPublisher sut = new MediatorDomainEventPublisher(publisher);
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
}
