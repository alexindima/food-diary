using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Notifications.Common;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Events;
using FoodDiary.Mediator;
using FoodDiary.Domain.Entities.Notifications;

namespace FoodDiary.Application.Dietologist.EventHandlers;

public class RecommendationCreatedEventHandler(
    INotificationRepository notificationRepository,
    INotificationWriter notificationWriter,
    INotificationPusher notificationPusher,
    IUserRepository userRepository)
    : INotificationHandler<NotificationEnvelope<RecommendationCreatedDomainEvent>> {
    public async Task Handle(NotificationEnvelope<RecommendationCreatedDomainEvent> envelope, CancellationToken cancellationToken) {
        RecommendationCreatedDomainEvent domainEvent = envelope.Value;
        User? dietologist = await userRepository.GetByIdAsync(domainEvent.DietologistUserId, cancellationToken).ConfigureAwait(false);
        string dietologistName = ResolveDietologistLabel(dietologist);

        Notification notification = NotificationFactory.CreateNewRecommendation(
            domainEvent.ClientUserId,
            dietologistName,
            domainEvent.RecommendationId.Value.ToString());

        await notificationWriter.AddAsync(notification, cancellationToken: cancellationToken).ConfigureAwait(false);

        int unreadCount = await notificationRepository.GetUnreadCountAsync(domainEvent.ClientUserId, cancellationToken).ConfigureAwait(false);
        await notificationPusher.PushUnreadCountAsync(domainEvent.ClientUserId.Value, unreadCount, cancellationToken).ConfigureAwait(false);
    }

    private static string ResolveDietologistLabel(User? dietologist) {
        if (dietologist is null) {
            return string.Empty;
        }

        string fullName = $"{dietologist.FirstName} {dietologist.LastName}".Trim();

        return string.IsNullOrWhiteSpace(fullName)
            ? dietologist.Email
            : $"{fullName} ({dietologist.Email})";
    }
}
