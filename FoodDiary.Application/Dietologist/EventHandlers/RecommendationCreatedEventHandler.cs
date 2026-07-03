using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Dietologist.Common;
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
    IDietologistUserLookupService userLookupService,
    IPostCommitActionQueue postCommitActionQueue)
    : INotificationHandler<NotificationEnvelope<RecommendationCreatedDomainEvent>> {
    public async Task Handle(NotificationEnvelope<RecommendationCreatedDomainEvent> envelope, CancellationToken cancellationToken) {
        RecommendationCreatedDomainEvent domainEvent = envelope.Value;
        User? dietologist = await userLookupService.GetUserByIdAsync(domainEvent.DietologistUserId, cancellationToken).ConfigureAwait(false);
        string dietologistName = ResolveDietologistLabel(dietologist);

        Notification notification = NotificationFactory.CreateNewRecommendation(
            domainEvent.ClientUserId,
            dietologistName,
            domainEvent.RecommendationId.Value.ToString());

        await notificationWriter.AddAsync(notification, cancellationToken: cancellationToken).ConfigureAwait(false);
        NotificationPostCommitActions.EnqueueUnreadCountPush(
            postCommitActionQueue,
            notificationRepository,
            notificationPusher,
            domainEvent.ClientUserId,
            pushChanged: false);
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
