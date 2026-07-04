using System.Globalization;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.Fasting.Common;
using FoodDiary.Application.Notifications.Common;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.Extensions.Logging;
using FoodDiary.Domain.Entities.Tracking.Fasting;
using FoodDiary.Domain.Entities.Notifications;

namespace FoodDiary.Application.Fasting.Services;

public sealed class FastingNotificationScheduler(
    IFastingOccurrenceReadRepository fastingOccurrenceRepository,
    IFastingCheckInReadRepository fastingCheckInRepository,
    INotificationReadRepository notificationRepository,
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
        var checkInLookup = checkIns
            .GroupBy(static x => x.OccurrenceId)
            .ToDictionary(static group => group.Key, static group => (IReadOnlyList<FastingCheckIn>)[.. group]);
        var usersToPush = new HashSet<Guid>();
        int createdCount = 0;

        foreach (FastingOccurrence occurrence in activeOccurrences) {
            FastingPlan? plan = occurrence.Plan;
            if (plan is null || plan.Status != FastingPlanStatus.Active) {
                continue;
            }

            checkInLookup.TryGetValue(occurrence.Id, out IReadOnlyList<FastingCheckIn>? occurrenceCheckIns);
            createdCount += await ProcessCheckInReminderNotificationsAsync(occurrence, occurrenceCheckIns, now, usersToPush, cancellationToken).ConfigureAwait(false);
            createdCount += plan.Type switch {
                FastingPlanType.Intermittent => await ProcessIntermittentNotificationsAsync(occurrence, plan, now, usersToPush, cancellationToken).ConfigureAwait(false),
                _ => await ProcessCompletionNotificationAsync(occurrence, plan, now, usersToPush, cancellationToken)
.ConfigureAwait(false),
            };
        }

        if (createdCount > 0) {
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        foreach (Guid userGuid in usersToPush) {
            var userId = new UserId(userGuid);
            int unreadCount = await notificationRepository.GetUnreadCountAsync(userId, cancellationToken).ConfigureAwait(false);
            await notificationPusher.PushUnreadCountAsync(userGuid, unreadCount, cancellationToken).ConfigureAwait(false);
            await notificationPusher.PushNotificationsChangedAsync(userGuid, cancellationToken).ConfigureAwait(false);
        }

        if (postCommitActionQueue.HasActions) {
            await postCommitActionQueue.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        if (createdCount > 0) {
            logger.LogInformation(
                "Created {NotificationCount} fasting notifications for {UserCount} users.",
                createdCount,
                usersToPush.Count);
        }

        return createdCount;
    }

    private async Task<int> ProcessCheckInReminderNotificationsAsync(
        FastingOccurrence occurrence,
        IReadOnlyList<FastingCheckIn>? checkIns,
        DateTime now,
        ISet<Guid> usersToPush,
        CancellationToken cancellationToken) {
        if (HasExistingCheckIn(occurrence, checkIns)) {
            return 0;
        }

        TimeSpan elapsed = now - occurrence.StartedAtUtc;
        if (elapsed < TimeSpan.Zero) {
            return 0;
        }

        int createdCount = 0;
        int[] reminderHours = [.. new[] {
            occurrence.User.FastingCheckInReminderHours,
            occurrence.User.FastingCheckInFollowUpReminderHours,
        }.Distinct().Order()];

        foreach (int hour in reminderHours) {
            if (elapsed.TotalHours < hour) {
                continue;
            }

            createdCount += await TryCreateCheckInReminderAsync(
                occurrence,
                string.Create(CultureInfo.InvariantCulture, $"fasting-check-in-reminder:{occurrence.Id.Value}:{hour}"),
                usersToPush,
                cancellationToken).ConfigureAwait(false);
        }

        return createdCount;
    }

    private static bool HasExistingCheckIn(FastingOccurrence occurrence, IReadOnlyList<FastingCheckIn>? checkIns) {
        return checkIns is { Count: > 0 } || occurrence.CheckInAtUtc.HasValue;
    }

    private async Task<int> ProcessCompletionNotificationAsync(
        FastingOccurrence occurrence,
        FastingPlan plan,
        DateTime now,
        ISet<Guid> usersToPush,
        CancellationToken cancellationToken) {
        if (!occurrence.TargetHours.HasValue) {
            return 0;
        }

        DateTime completionAtUtc = occurrence.StartedAtUtc.AddHours(occurrence.TargetHours.Value);
        if (completionAtUtc > now) {
            return 0;
        }

        string referenceId = $"fasting-completed:{occurrence.Id.Value}";
        if (await notificationRepository.ExistsAsync(occurrence.UserId, NotificationTypes.FastingCompleted, referenceId, cancellationToken).ConfigureAwait(false)) {
            return 0;
        }

        Notification notification = NotificationFactory.CreateFastingCompleted(
            occurrence.UserId,
            plan.Type.ToString(),
            occurrence.Kind.ToString(),
            referenceId);

        await notificationWriter.AddAsync(notification, sendWebPush: true, cancellationToken).ConfigureAwait(false);
        usersToPush.Add(occurrence.UserId.Value);
        return 1;
    }

    private async Task<int> TryCreateCheckInReminderAsync(
        FastingOccurrence occurrence,
        string referenceId,
        ISet<Guid> usersToPush,
        CancellationToken cancellationToken) {
        if (await notificationRepository.ExistsAsync(
                occurrence.UserId,
                NotificationTypes.FastingCheckInReminder,
                referenceId,
                cancellationToken).ConfigureAwait(false)) {
            return 0;
        }

        Notification notification = NotificationFactory.CreateFastingCheckInReminder(occurrence.UserId, referenceId);

        await notificationWriter.AddAsync(notification, sendWebPush: true, cancellationToken).ConfigureAwait(false);
        usersToPush.Add(occurrence.UserId.Value);
        return 1;
    }

    private async Task<int> ProcessIntermittentNotificationsAsync(
        FastingOccurrence occurrence,
        FastingPlan plan,
        DateTime now,
        ISet<Guid> usersToPush,
        CancellationToken cancellationToken) {
        int? fastHours = plan.IntermittentFastHours ?? occurrence.TargetHours;
        int? eatingWindowHours = plan.IntermittentEatingWindowHours;
        if (!fastHours.HasValue || !eatingWindowHours.HasValue) {
            return 0;
        }

        int createdCount = 0;
        int cycleLengthHours = fastHours.Value + eatingWindowHours.Value;
        TimeSpan elapsed = now - occurrence.StartedAtUtc;
        if (elapsed < TimeSpan.Zero) {
            return 0;
        }

        int completedCycles = (int)Math.Floor(elapsed.TotalHours / cycleLengthHours);
        for (int cycleIndex = 0; cycleIndex <= completedCycles; cycleIndex++) {
            DateTime eatingWindowStartUtc = occurrence.StartedAtUtc.AddHours((cycleIndex * cycleLengthHours) + fastHours.Value);
            if (eatingWindowStartUtc <= now) {
                createdCount += await TryCreateNotificationAsync(
                    occurrence,
                    plan,
                    NotificationTypes.EatingWindowStarted,
                    string.Create(CultureInfo.InvariantCulture, $"eating-window-started:{occurrence.Id.Value}:{cycleIndex + 1}"),
                    usersToPush,
                    cancellationToken).ConfigureAwait(false);
            }

            DateTime fastingWindowStartUtc = occurrence.StartedAtUtc.AddHours((cycleIndex + 1) * cycleLengthHours);
            if (fastingWindowStartUtc <= now) {
                createdCount += await TryCreateNotificationAsync(
                    occurrence,
                    plan,
                    NotificationTypes.FastingWindowStarted,
                    $"fasting-window-started:{occurrence.Id.Value}:{(cycleIndex + 2).ToString(CultureInfo.InvariantCulture)}",
                    usersToPush,
                    cancellationToken).ConfigureAwait(false);
            }
        }

        return createdCount;
    }

    private async Task<int> TryCreateNotificationAsync(
        FastingOccurrence occurrence,
        FastingPlan plan,
        string notificationType,
        string referenceId,
        ISet<Guid> usersToPush,
        CancellationToken cancellationToken) {
        if (await notificationRepository.ExistsAsync(occurrence.UserId, notificationType, referenceId, cancellationToken).ConfigureAwait(false)) {
            return 0;
        }

        Notification notification = notificationType switch {
            NotificationTypes.EatingWindowStarted => NotificationFactory.CreateEatingWindowStarted(
                occurrence.UserId,
                plan.Type.ToString(),
                occurrence.Kind.ToString(),
                referenceId),
            NotificationTypes.FastingWindowStarted => NotificationFactory.CreateFastingWindowStarted(
                occurrence.UserId,
                plan.Type.ToString(),
                occurrence.Kind.ToString(),
                referenceId),
            _ => throw new InvalidOperationException($"Unsupported fasting notification type '{notificationType}'."),
        };

        await notificationWriter.AddAsync(notification, sendWebPush: true, cancellationToken).ConfigureAwait(false);
        usersToPush.Add(occurrence.UserId.Value);
        return 1;
    }
}
