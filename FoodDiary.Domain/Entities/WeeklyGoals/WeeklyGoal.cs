using FoodDiary.Domain.Enums;
using FoodDiary.Domain.Primitives;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.WeeklyGoals;

public sealed class WeeklyGoal : Entity<WeeklyGoalId> {
    private static readonly int[] SupportedTargetDays = [3, 5, 7];
    private const int MinimumTimeZoneOffsetMinutes = -14 * 60;
    private const int MaximumTimeZoneOffsetMinutes = 14 * 60;
    private const int MinutesPerDay = 24 * 60;

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
        DateTime normalizedWeekStart = NormalizeWeekStart(weekStartUtc);
        ValidateType(type);
        ValidateTargetDays(targetDays);
        ValidateReminder(reminderEnabled, reminderTimeMinutes, timeZoneOffsetMinutes);

        var goal = new WeeklyGoal {
            Id = WeeklyGoalId.New(),
            UserId = userId,
            WeekStartUtc = normalizedWeekStart,
            Type = type,
            TargetDays = targetDays,
            ReminderEnabled = reminderEnabled,
            ReminderTimeMinutes = reminderEnabled ? reminderTimeMinutes : null,
            TimeZoneOffsetMinutes = reminderEnabled ? timeZoneOffsetMinutes : null,
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
        ValidateReminder(reminderEnabled, reminderTimeMinutes, timeZoneOffsetMinutes);
        DateTime normalizedModifiedAtUtc = NormalizeUtc(modifiedAtUtc);

        int? normalizedReminderTimeMinutes = reminderEnabled ? reminderTimeMinutes : null;
        int? normalizedTimeZoneOffsetMinutes = reminderEnabled ? timeZoneOffsetMinutes : null;
        bool reminderConfigurationChanged =
            ReminderEnabled != reminderEnabled ||
            ReminderTimeMinutes != normalizedReminderTimeMinutes ||
            TimeZoneOffsetMinutes != normalizedTimeZoneOffsetMinutes;
        if (TargetDays == targetDays && !reminderConfigurationChanged) {
            return;
        }

        TargetDays = targetDays;
        ReminderEnabled = reminderEnabled;
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

        LastReminderLocalDate = localDate;
        SetModified(NormalizeUtc(modifiedAtUtc));
    }

    private static void EnsureUserId(UserId userId) {
        if (userId == UserId.Empty) {
            throw new ArgumentException("User id is required.", nameof(userId));
        }
    }

    private static DateTime NormalizeWeekStart(DateTime value) {
        DateTime utc = NormalizeUtc(value).Date;
        if (utc.DayOfWeek != DayOfWeek.Monday) {
            throw new ArgumentOutOfRangeException(nameof(value), "Week start must be a Monday.");
        }

        return utc;
    }

    private static DateTime NormalizeUtc(DateTime value) =>
        value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
            : value.ToUniversalTime();

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

    private static void ValidateReminder(bool enabled, int? timeMinutes, int? offsetMinutes) {
        if (!enabled) {
            return;
        }

        if (timeMinutes is null or < 0 or >= MinutesPerDay) {
            throw new ArgumentOutOfRangeException(nameof(timeMinutes), "Reminder time must be within a local day.");
        }

        if (offsetMinutes is null or < MinimumTimeZoneOffsetMinutes or > MaximumTimeZoneOffsetMinutes) {
            throw new ArgumentOutOfRangeException(nameof(offsetMinutes), "Time zone offset must be between UTC-14 and UTC+14.");
        }
    }
}
