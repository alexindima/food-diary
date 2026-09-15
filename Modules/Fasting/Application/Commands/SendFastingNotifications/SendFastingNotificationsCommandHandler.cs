using FoodDiary.Modules.Fasting.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Fasting.Domain.Enums;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Modules.Notifications.Contracts.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.Extensions.Logging;
using FoodDiary.Modules.Fasting.Domain.Entities.Tracking.Fasting;
using FoodDiary.Mediator;
using FoodDiary.Modules.Fasting.Contracts.Commands.SendFastingNotifications;
using FoodDiary.Modules.Fasting.Application.Services;

namespace FoodDiary.Modules.Fasting.Application.Commands.SendFastingNotifications;

public sealed class SendFastingNotificationsCommandHandler(IFastingOccurrenceReadRepository fastingOccurrenceRepository,
    IFastingCheckInReadRepository fastingCheckInRepository,
    INotificationDeduplicationService notificationDeduplicationService,
    INotificationWriter notificationWriter,
    INotificationClientRefreshService notificationClientRefreshService,
    IUnitOfWork unitOfWork,
    IPostCommitActionQueue postCommitActionQueue,
    TimeProvider dateTimeProvider,
    ILogger<SendFastingNotificationsCommandHandler> logger) : IRequestHandler<SendFastingNotificationsCommand, int> {
    public async Task<int> Handle(SendFastingNotificationsCommand request, CancellationToken cancellationToken) {
        DateTime now = dateTimeProvider.GetUtcNow().UtcDateTime;
        IReadOnlyList<FastingActiveOccurrenceModel> activeOccurrences = await fastingOccurrenceRepository.GetActiveAsync(cancellationToken).ConfigureAwait(false);
        FastingOccurrenceId[] activeOccurrenceIds = [.. activeOccurrences.Select(static x => x.Occurrence.Id)];
        IReadOnlyList<FastingCheckIn> checkIns = activeOccurrenceIds.Length == 0
            ? []
            : await fastingCheckInRepository.GetByOccurrenceIdsAsync(activeOccurrenceIds, cancellationToken).ConfigureAwait(false);
        IReadOnlyDictionary<FastingOccurrenceId, IReadOnlyList<FastingCheckIn>> checkInLookup =
            FastingCheckInLookup.Create(checkIns);
        var usersToPush = new HashSet<UserId>();
        int createdCount = 0;

        foreach (FastingActiveOccurrenceModel active in activeOccurrences) {
            FastingOccurrence occurrence = active.Occurrence;
            FastingPlan? plan = occurrence.Plan;
            if (plan is null || plan.Status != FastingPlanStatus.Active) {
                continue;
            }

            checkInLookup.TryGetValue(occurrence.Id, out IReadOnlyList<FastingCheckIn>? occurrenceCheckIns);
            foreach (FastingNotificationCandidate notification in FastingNotificationCandidatePlanner.GetDueNotifications(occurrence, plan, occurrenceCheckIns, now, active.ReminderHours, active.FollowUpReminderHours)) {
                bool created = await TryCreateAsync(
                    notification,
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
            postCommitActionQueue.Enqueue("fasting.notifications.push", ct => PushAsync(
                pushUserIds,
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

    private async Task<bool> TryCreateAsync(
        FastingNotificationCandidate candidate,
        CancellationToken cancellationToken) {
        if (await notificationDeduplicationService.ExistsAsync(
                candidate.UserId,
                candidate.Type,
                candidate.ReferenceId,
                cancellationToken).ConfigureAwait(false)) {
            return false;
        }

        NotificationRequest notification = FastingNotificationFactory.Create(candidate);
        await notificationWriter.AddAsync(notification, sendWebPush: true, cancellationToken).ConfigureAwait(false);
        return true;
    }

    private async Task PushAsync(
        IReadOnlyCollection<UserId> usersToPush,
        CancellationToken cancellationToken) {
        foreach (UserId userId in usersToPush) {
            await notificationClientRefreshService
                .RefreshAsync(userId, pushChanged: true, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
