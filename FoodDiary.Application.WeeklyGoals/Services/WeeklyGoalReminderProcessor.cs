using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.WeeklyGoals.Common;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.Entities.WeeklyGoals;

namespace FoodDiary.Application.WeeklyGoals.Services;

public sealed class WeeklyGoalReminderProcessor(
    IWeeklyGoalRepository goalRepository,
    INotificationWriter notificationWriter,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) {
    private const int BatchSize = 500;
    private static readonly TimeSpan DueWindow = TimeSpan.FromMinutes(15);

    public async Task<int> ProcessAsync(CancellationToken cancellationToken = default) {
        DateTime utcNow = timeProvider.GetUtcNow().UtcDateTime;
        IReadOnlyList<WeeklyGoal> candidates = await goalRepository.GetReminderCandidatesAsync(
            StartOfWeek(utcNow.AddHours(-14)),
            StartOfWeek(utcNow.AddHours(14)),
            BatchSize,
            cancellationToken).ConfigureAwait(false);
        int sent = 0;

        foreach (WeeklyGoal goal in candidates) {
            if (!TryGetDueLocalDate(goal, utcNow, out DateOnly localDate)) {
                continue;
            }

            await notificationWriter.AddAsync(
                Notification.Create(
                    goal.UserId,
                    NotificationTypes.WeeklyGoalReminder,
                    NotificationPayloads.Empty(),
                    goal.Id.Value.ToString()),
                sendWebPush: true,
                cancellationToken).ConfigureAwait(false);
            goal.MarkReminderSent(localDate, utcNow);
            sent++;
        }

        if (sent > 0) {
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return sent;
    }

    private static bool TryGetDueLocalDate(WeeklyGoal goal, DateTime utcNow, out DateOnly localDate) {
        localDate = default;
        if (goal.ReminderTimeMinutes is not { } reminderMinutes || goal.TimeZoneOffsetMinutes is not { } offsetMinutes) {
            return false;
        }

        DateTime localNow = utcNow.AddMinutes(offsetMinutes);
        localDate = DateOnly.FromDateTime(localNow);
        var weekStart = DateOnly.FromDateTime(goal.WeekStartUtc);
        if (localDate < weekStart || localDate > weekStart.AddDays(6) || goal.LastReminderLocalDate == localDate) {
            return false;
        }

        TimeSpan elapsed = localNow.TimeOfDay - TimeSpan.FromMinutes(reminderMinutes);
        return elapsed >= TimeSpan.Zero && elapsed < DueWindow;
    }

    private static DateTime StartOfWeek(DateTime value) {
        DateTime date = value.Date;
        int daysSinceMonday = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return DateTime.SpecifyKind(date.AddDays(-daysSinceMonday), DateTimeKind.Utc);
    }
}
