using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Modules.Dietologist.Domain.Events;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Modules.Dietologist.Application.Common;
using FoodDiary.Modules.Notifications.Contracts.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.Dietologist.Application.EventHandlers;

public sealed class RecommendationCreatedEventHandler(
    INotificationClientRefreshService notificationClientRefreshService,
    INotificationWriter notificationWriter,
    IUserDietologistProfileReadService userLookupService,
    IPostCommitActionQueue postCommitActionQueue)
    : INotificationHandler<NotificationEnvelope<RecommendationCreatedDomainEvent>> {
    public async Task Handle(NotificationEnvelope<RecommendationCreatedDomainEvent> notification, CancellationToken cancellationToken) {
        RecommendationCreatedDomainEvent domainEvent = notification.Value;
        UserDietologistProfileModel? dietologist = await userLookupService.FindByIdAsync(domainEvent.DietologistUserId, cancellationToken).ConfigureAwait(false);
        string dietologistName = ResolveDietologistLabel(dietologist);

        NotificationRequest createdNotification = DietologistNotificationFactory.CreateNewRecommendation(
            domainEvent.ClientUserId,
            dietologistName,
            domainEvent.RecommendationId.Value.ToString());

        await notificationWriter.AddAsync(createdNotification, cancellationToken: cancellationToken).ConfigureAwait(false);
        DietologistNotificationPostCommitActions.EnqueueUnreadCountPush(
            postCommitActionQueue,
            notificationClientRefreshService,
            domainEvent.ClientUserId,
            pushChanged: false);
    }

    private static string ResolveDietologistLabel(UserDietologistProfileModel? dietologist) {
        if (dietologist is null) {
            return string.Empty;
        }

        string fullName = $"{dietologist.FirstName} {dietologist.LastName}".Trim();

        return string.IsNullOrWhiteSpace(fullName) || dietologist.Email is null
            ? DietologistProfileDisplayName.Resolve(dietologist)
            : $"{fullName} ({dietologist.Email})";
    }
}
