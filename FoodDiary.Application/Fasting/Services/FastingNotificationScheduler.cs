using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.Fasting.Common;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.Extensions.Logging;
using FoodDiary.Domain.Entities.Tracking.Fasting;

namespace FoodDiary.Application.Fasting.Services;

public sealed class FastingNotificationScheduler(
    IFastingOccurrenceReadRepository fastingOccurrenceRepository,
    IFastingCheckInReadRepository fastingCheckInRepository,
    INotificationLookupRepository notificationLookupRepository,
    INotificationReadModelRepository notificationReadModelRepository,
    INotificationWriter notificationWriter,
    INotificationPusher notificationPusher,
    IUnitOfWork unitOfWork,
    IPostCommitActionQueue postCommitActionQueue,
    TimeProvider dateTimeProvider,
    ILogger<FastingNotificationScheduler> logger)
    : IFastingNotificationScheduler {

    public async Task<int> ProcessDueNotificationsAsync(CancellationToken cancellationToken = default) {
        DateTime now = dateTimeProvider.GetUtcNow().UtcDateTime;
        IReadOnlyList<FastingOccurrence> activeOccurrences = await fastingOccurrenceRepository.GetActiveAsync(cancellationToken).ConfigureAwait(false);
        FastingOccurrenceId[] activeOccurrenceIds = [.. activeOccurrences.Select(static x => x.Id)];
        IReadOnlyList<FastingCheckIn> checkIns = activeOccurrenceIds.Length == 0
            ? []
            : await fastingCheckInRepository.GetByOccurrenceIdsAsync(activeOccurrenceIds, cancellationToken).ConfigureAwait(false);
        IReadOnlyDictionary<FastingOccurrenceId, IReadOnlyList<FastingCheckIn>> checkInLookup =
            FastingCheckInLookup.Create(checkIns);
        var usersToPush = new HashSet<UserId>();
        int createdCount = 0;

        foreach (FastingOccurrence occurrence in activeOccurrences) {
            FastingPlan? plan = occurrence.Plan;
            if (plan is null || plan.Status != FastingPlanStatus.Active) {
                continue;
            }

            checkInLookup.TryGetValue(occurrence.Id, out IReadOnlyList<FastingCheckIn>? occurrenceCheckIns);
            foreach (FastingNotificationCandidate notification in FastingNotificationCandidatePlanner.GetDueNotifications(occurrence, plan, occurrenceCheckIns, now)) {
                bool created = await FastingNotificationCreationService.TryCreateAsync(
                    notification,
                    notificationLookupRepository,
                    notificationWriter,
                    cancellationToken).ConfigureAwait(false);

                if (created) {
                    usersToPush.Add(notification.UserId);
                    createdCount++;
                }
            }
        }

        if (createdCount > 0) {
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        if (usersToPush.Count > 0) {
            UserId[] pushUserIds = [.. usersToPush];
            postCommitActionQueue.Enqueue("fasting.notifications.push", ct => FastingNotificationPushDispatcher.PushAsync(
                pushUserIds,
                notificationReadModelRepository,
                notificationPusher,
                ct));
        }

        if (postCommitActionQueue.HasActions) {
            await postCommitActionQueue.FlushAsync(CancellationToken.None).ConfigureAwait(false);
        }

        if (createdCount > 0) {
            logger.LogInformation(
                "Created {NotificationCount} fasting notifications for {UserCount} users.",
                createdCount,
                usersToPush.Count);
        }

        return createdCount;
    }
}
