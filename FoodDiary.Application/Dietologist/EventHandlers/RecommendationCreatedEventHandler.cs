using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Notifications.Common;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Domain.Events;
using FoodDiary.Mediator;

namespace FoodDiary.Application.Dietologist.EventHandlers;

public class RecommendationCreatedEventHandler(
    INotificationRepository notificationRepository,
    INotificationPusher notificationPusher,
    IUserRepository userRepository)
    : INotificationHandler<RecommendationCreatedNotification> {
    public async Task Handle(RecommendationCreatedNotification wrapper, CancellationToken cancellationToken) {
        var domainEvent = wrapper.DomainEvent;
        var dietologist = await userRepository.GetByIdAsync(domainEvent.DietologistUserId, cancellationToken);
        var dietologistName = dietologist is not null
            ? $"{dietologist.FirstName} {dietologist.LastName}".Trim()
            : "Your dietologist";

        if (string.IsNullOrWhiteSpace(dietologistName)) {
            dietologistName = "Your dietologist";
        }

        var notification = NotificationFactory.CreateNewRecommendation(
            domainEvent.ClientUserId,
            dietologistName,
            domainEvent.RecommendationId.Value.ToString());

        await notificationRepository.AddAsync(notification, cancellationToken);

        var unreadCount = await notificationRepository.GetUnreadCountAsync(domainEvent.ClientUserId, cancellationToken);
        await notificationPusher.PushUnreadCountAsync(domainEvent.ClientUserId.Value, unreadCount, cancellationToken);
    }
}

public sealed record RecommendationCreatedNotification(RecommendationCreatedDomainEvent DomainEvent) : INotification;
