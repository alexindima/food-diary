using FoodDiary.Application.Abstractions.Common.Interfaces.Services;
using FoodDiary.Application.Abstractions.Fasting.Common;
using FoodDiary.Application.Notifications.Common;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.Extensions.Logging;
using FoodDiary.Domain.Entities.Tracking.Fasting;

namespace FoodDiary.Application.Fasting.Services;

public sealed class FastingNotificationScheduler(
    IFastingOccurrenceRepository fastingOccurrenceRepository,
    IFastingCheckInRepository fastingCheckInRepository,
    INotificationRepository notificationRepository,
    INotificationPusher notificationPusher,
    IWebPushNotificationSender webPushNotificationSender,
    IDateTimeProvider dateTimeProvider,
    ILogger<FastingNotificationScheduler> logger)
    : IFastingNotificationScheduler {

    public async Task<int> ProcessDueNotificationsAsync(CancellationToken cancellationToken = default) {
        var now = dateTimeProvider.UtcNow;
        var activeOccurrences = await fastingOccurrenceRepository.GetActiveAsync(cancellationToken);
        var activeOccurrenceIds = activeOccurrences.Select(static x => x.Id).ToArray();
        var checkIns = activeOccurrenceIds.Length == 0
            ? []
            : await fastingCheckInRepository.GetByOccurrenceIdsAsync(activeOccurrenceIds, cancellationToken);
        var checkInLookup = checkIns
            .GroupBy(static x => x.OccurrenceId)
            .ToDictionary(static group => group.Key, static group => (IReadOnlyList<FastingCheckIn>)group.ToList());
        var usersToPush = new HashSet<Guid>();
        var createdCount = 0;

        foreach (var occurrence in activeOccurrences) {
            var plan = occurrence.Plan;
            if (plan is null || plan.Status != FastingPlanStatus.Active) {
                continue;
            }

            checkInLookup.TryGetValue(occurrence.Id, out var occurrenceCheckIns);
            createdCount += await ProcessCheckInReminderNotificationsAsync(occurrence, occurrenceCheckIns, now, usersToPush, cancellationToken);
            createdCount += plan.Type switch {
                FastingPlanType.Intermittent => await ProcessIntermittentNotificationsAsync(occurrence, plan, now, usersToPush, cancellationToken),
                _ => await ProcessCompletionNotificationAsync(occurrence, plan, now, usersToPush, cancellationToken)
            };
        }

        foreach (var userGuid in usersToPush) {
            var userId = new UserId(userGuid);
            var unreadCount = await notificationRepository.GetUnreadCountAsync(userId, cancellationToken);
            await notificationPusher.PushUnreadCountAsync(userGuid, unreadCount, cancellationToken);
            await notificationPusher.PushNotificationsChangedAsync(userGuid, cancellationToken);
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

        var elapsed = now - occurrence.StartedAtUtc;
        if (elapsed < TimeSpan.Zero) {
            return 0;
        }

        var createdCount = 0;
        var reminderHours = new[] {
            occurrence.User.FastingCheckInReminderHours,
            occurrence.User.FastingCheckInFollowUpReminderHours,
        }
            .Distinct()
            .OrderBy(static hour => hour)
            .ToArray();

        foreach (var hour in reminderHours) {
            if (elapsed.TotalHours < hour) {
                continue;
            }

            createdCount += await TryCreateCheckInReminderAsync(
                occurrence,
                $"fasting-check-in-reminder:{occurrence.Id.Value}:{hour}",
                usersToPush,
                cancellationToken);
        }

        return createdCount;
    }

    private static bool HasExistingCheckIn(FastingOccurrence occurrence, IReadOnlyList<FastingCheckIn>? checkIns) {
        if (checkIns is { Count: > 0 }) {
            return true;
        }

        return occurrence.CheckInAtUtc.HasValue;
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

        var completionAtUtc = occurrence.StartedAtUtc.AddHours(occurrence.TargetHours.Value);
        if (completionAtUtc > now) {
            return 0;
        }

        var referenceId = $"fasting-completed:{occurrence.Id.Value}";
        if (await notificationRepository.ExistsAsync(occurrence.UserId, NotificationTypes.FastingCompleted, referenceId, cancellationToken)) {
            return 0;
        }

        var notification = NotificationFactory.CreateFastingCompleted(
            occurrence.UserId,
            plan.Type.ToString(),
            occurrence.Kind.ToString(),
            referenceId);

        await notificationRepository.AddAsync(notification, cancellationToken);
        await webPushNotificationSender.SendAsync(notification, cancellationToken);
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
                cancellationToken)) {
            return 0;
        }

        var notification = NotificationFactory.CreateFastingCheckInReminder(occurrence.UserId, referenceId);

        await notificationRepository.AddAsync(notification, cancellationToken);
        await webPushNotificationSender.SendAsync(notification, cancellationToken);
        usersToPush.Add(occurrence.UserId.Value);
        return 1;
    }

    private async Task<int> ProcessIntermittentNotificationsAsync(
        FastingOccurrence occurrence,
        FastingPlan plan,
        DateTime now,
        ISet<Guid> usersToPush,
        CancellationToken cancellationToken) {
        var fastHours = plan.IntermittentFastHours ?? occurrence.TargetHours;
        var eatingWindowHours = plan.IntermittentEatingWindowHours;
        if (!fastHours.HasValue || !eatingWindowHours.HasValue) {
            return 0;
        }

        var createdCount = 0;
        var cycleLengthHours = fastHours.Value + eatingWindowHours.Value;
        var elapsed = now - occurrence.StartedAtUtc;
        if (elapsed < TimeSpan.Zero) {
            return 0;
        }

        var completedCycles = (int)Math.Floor(elapsed.TotalHours / cycleLengthHours);
        for (var cycleIndex = 0; cycleIndex <= completedCycles; cycleIndex++) {
            var eatingWindowStartUtc = occurrence.StartedAtUtc.AddHours((cycleIndex * cycleLengthHours) + fastHours.Value);
            if (eatingWindowStartUtc <= now) {
                createdCount += await TryCreateNotificationAsync(
                    occurrence,
                    plan,
                    NotificationTypes.EatingWindowStarted,
                    $"eating-window-started:{occurrence.Id.Value}:{cycleIndex + 1}",
                    usersToPush,
                    cancellationToken);
            }

            var fastingWindowStartUtc = occurrence.StartedAtUtc.AddHours((cycleIndex + 1) * cycleLengthHours);
            if (fastingWindowStartUtc <= now) {
                createdCount += await TryCreateNotificationAsync(
                    occurrence,
                    plan,
                    NotificationTypes.FastingWindowStarted,
                    $"fasting-window-started:{occurrence.Id.Value}:{cycleIndex + 2}",
                    usersToPush,
                    cancellationToken);
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
        if (await notificationRepository.ExistsAsync(occurrence.UserId, notificationType, referenceId, cancellationToken)) {
            return 0;
        }

        var notification = notificationType switch {
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
            _ => throw new InvalidOperationException($"Unsupported fasting notification type '{notificationType}'.")
        };

        await notificationRepository.AddAsync(notification, cancellationToken);
        await webPushNotificationSender.SendAsync(notification, cancellationToken);
        usersToPush.Add(occurrence.UserId.Value);
        return 1;
    }
}
