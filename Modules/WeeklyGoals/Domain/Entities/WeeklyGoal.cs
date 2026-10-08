using FoodDiary.Modules.WeeklyGoals.Domain.Enums;
using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.WeeklyGoals.Domain.ValueObjects;
using FoodDiary.Modules.WeeklyGoals.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.WeeklyGoals.Domain.Entities;

public sealed class WeeklyGoal : Entity<WeeklyGoalId> {
    private static readonly int[] SupportedTargetDays = [3, 5, 7];

    public UserId UserId { get; private set; }
    public DateTime WeekStartUtc { get; private set; }
    public WeeklyGoalType Type { get; private set; }
    public int TargetDays { get; private set; }
    public bool ReminderEnabled { get; private set; }
    public int? ReminderTimeMinutes { get; private set; }
    public int? TimeZoneOffsetMinutes { get; private set; }
    public DateOnly? LastReminderLocalDate { get; private set; }

    private WeeklyGoal() {
    }

    public static WeeklyGoal Create(
        UserId userId,
        DateTime weekStartUtc,
        WeeklyGoalType type,
        int targetDays,
        bool reminderEnabled,
        int? reminderTimeMinutes,
        int? timeZoneOffsetMinutes) {
        EnsureUserId(userId);
        _ = NormalizeWeekStart(weekStartUtc);
        ValidateType(type);
        ValidateTargetDays(targetDays);
        var reminder = WeeklyGoalReminderSettings.FromMinutes(reminderEnabled, reminderTimeMinutes, timeZoneOffsetMinutes);
        return CreateWithReminder(userId, weekStartUtc, type, targetDays, reminder);
    }

    public static WeeklyGoal CreateWithReminder(
        UserId userId,
        DateTime weekStartUtc,
        WeeklyGoalType type,
        int targetDays,
        WeeklyGoalReminderSettings reminder) {
        EnsureUserId(userId);
        DateTime normalizedWeekStart = NormalizeWeekStart(weekStartUtc);
        ValidateType(type);
        ValidateTargetDays(targetDays);
        ArgumentNullException.ThrowIfNull(reminder);

        var goal = new WeeklyGoal {
            Id = WeeklyGoalId.New(),
            UserId = userId,
            WeekStartUtc = normalizedWeekStart,
            Type = type,
            TargetDays = targetDays,
            ReminderEnabled = reminder.IsEnabled,
            ReminderTimeMinutes = reminder.LocalTimeMinutes,
            TimeZoneOffsetMinutes = reminder.UtcOffsetMinutes,
        };
        goal.SetCreated();
        return goal;
    }

    public void Update(
        int targetDays,
        bool reminderEnabled,
        int? reminderTimeMinutes,
        int? timeZoneOffsetMinutes,
        DateTime modifiedAtUtc) {
        ValidateTargetDays(targetDays);
        var reminder = WeeklyGoalReminderSettings.FromMinutes(reminderEnabled, reminderTimeMinutes, timeZoneOffsetMinutes);
        UpdateWithReminder(targetDays, reminder, modifiedAtUtc);
    }

    public void UpdateWithReminder(int targetDays, WeeklyGoalReminderSettings reminder, DateTime modifiedAtUtc) {
        ValidateTargetDays(targetDays);
        ArgumentNullException.ThrowIfNull(reminder);
        DateTime normalizedModifiedAtUtc = RequiredUtc(modifiedAtUtc, nameof(modifiedAtUtc));

        int? normalizedReminderTimeMinutes = reminder.LocalTimeMinutes;
        int? normalizedTimeZoneOffsetMinutes = reminder.UtcOffsetMinutes;
        bool reminderConfigurationChanged =
            ReminderEnabled != reminder.IsEnabled ||
            ReminderTimeMinutes != normalizedReminderTimeMinutes ||
            TimeZoneOffsetMinutes != normalizedTimeZoneOffsetMinutes;
        if (TargetDays == targetDays && !reminderConfigurationChanged) {
            return;
        }

        TargetDays = targetDays;
        ReminderEnabled = reminder.IsEnabled;
        ReminderTimeMinutes = normalizedReminderTimeMinutes;
        TimeZoneOffsetMinutes = normalizedTimeZoneOffsetMinutes;
        if (reminderConfigurationChanged) {
            LastReminderLocalDate = null;
        }

        SetModified(normalizedModifiedAtUtc);
    }

    public void MarkReminderSent(DateOnly localDate, DateTime modifiedAtUtc) {
        if (!ReminderEnabled) {
            throw new InvalidOperationException("A reminder cannot be marked for a goal with reminders disabled.");
        }

        DateTime normalizedModifiedAtUtc = RequiredUtc(modifiedAtUtc, nameof(modifiedAtUtc));
        ValidateReminderLocalDate(localDate, normalizedModifiedAtUtc);
        if (LastReminderLocalDate == localDate) {
            return;
        }

        LastReminderLocalDate = localDate;
        SetModified(normalizedModifiedAtUtc);
    }

    private void ValidateReminderLocalDate(DateOnly localDate, DateTime modifiedAtUtc) {
        if (TimeZoneOffsetMinutes is not { } offsetMinutes) {
            throw new InvalidOperationException("A reminder time zone offset is required.");
        }

        DateTime localTimestamp;
        try {
            localTimestamp = modifiedAtUtc.AddMinutes(offsetMinutes);
        } catch (ArgumentOutOfRangeException) {
            throw new ArgumentOutOfRangeException(
                nameof(modifiedAtUtc),
                modifiedAtUtc,
                "The reminder timestamp cannot be represented in the configured time zone.");
        }

        var expectedLocalDate = DateOnly.FromDateTime(localTimestamp);
        if (localDate != expectedLocalDate) {
            throw new ArgumentOutOfRangeException(nameof(localDate), "Reminder date must match the timestamp in the configured time zone.");
        }

        var weekStart = DateOnly.FromDateTime(WeekStartUtc);
        int dayOffset = localDate.DayNumber - weekStart.DayNumber;
        if (dayOffset is < 0 or > 6) {
            throw new ArgumentOutOfRangeException(nameof(localDate), "Reminder date must belong to the goal week.");
        }
    }

    private static void EnsureUserId(UserId userId) {
        if (userId == UserId.Empty) {
            throw new ArgumentException("User id is required.", nameof(userId));
        }
    }

    private static DateTime NormalizeWeekStart(DateTime value) {
        DateTime utc = value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value.Date, DateTimeKind.Utc)
            : value.ToUniversalTime().Date;
        if (utc.DayOfWeek != DayOfWeek.Monday) {
            throw new ArgumentOutOfRangeException(nameof(value), "Week start must be a Monday.");
        }

        return utc;
    }

    private static DateTime RequiredUtc(DateTime value, string paramName) {
        return value.Kind == DateTimeKind.Unspecified
            ? throw new ArgumentOutOfRangeException(paramName, "UTC timestamp kind must be specified.")
            : value.ToUniversalTime();
    }

    private static void ValidateType(WeeklyGoalType type) {
        if (type != WeeklyGoalType.DiaryLogging) {
            throw new ArgumentOutOfRangeException(nameof(type), "Unsupported weekly goal type.");
        }
    }

    private static void ValidateTargetDays(int targetDays) {
        if (!SupportedTargetDays.Contains(targetDays)) {
            throw new ArgumentOutOfRangeException(nameof(targetDays), "Target days must be 3, 5, or 7.");
        }
    }
}
