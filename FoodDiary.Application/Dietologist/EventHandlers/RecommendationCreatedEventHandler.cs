using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Notifications.Common;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Events;
using FoodDiary.Mediator;

namespace FoodDiary.Application.Dietologist.EventHandlers;

public class RecommendationCreatedEventHandler(
    INotificationRepository notificationRepository,
    INotificationPusher notificationPusher,
    IUserRepository userRepository)
    : INotificationHandler<NotificationEnvelope<RecommendationCreatedDomainEvent>> {
    public async Task Handle(NotificationEnvelope<RecommendationCreatedDomainEvent> envelope, CancellationToken cancellationToken) {
        var domainEvent = envelope.Value;
        var dietologist = await userRepository.GetByIdAsync(domainEvent.DietologistUserId, cancellationToken);
        var dietologistName = ResolveDietologistLabel(dietologist);

        var notification = NotificationFactory.CreateNewRecommendation(
            domainEvent.ClientUserId,
            dietologistName,
            domainEvent.RecommendationId.Value.ToString());

        await notificationRepository.AddAsync(notification, cancellationToken);

        var unreadCount = await notificationRepository.GetUnreadCountAsync(domainEvent.ClientUserId, cancellationToken);
        await notificationPusher.PushUnreadCountAsync(domainEvent.ClientUserId.Value, unreadCount, cancellationToken);
    }

    private static string ResolveDietologistLabel(User? dietologist) {
        if (dietologist is null) {
            return string.Empty;
        }

        var fullName = $"{dietologist.FirstName} {dietologist.LastName}".Trim();

        return string.IsNullOrWhiteSpace(fullName)
            ? dietologist.Email
            : $"{fullName} ({dietologist.Email})";
    }
}
