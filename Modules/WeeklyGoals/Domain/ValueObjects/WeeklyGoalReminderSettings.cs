namespace FoodDiary.Modules.WeeklyGoals.Domain.ValueObjects;

public sealed record WeeklyGoalReminderSettings {
    private const int MinutesPerDay = 24 * 60;
    private const int MaximumOffsetMinutes = 14 * 60;

    private WeeklyGoalReminderSettings(bool isEnabled, TimeOnly? localTime, TimeSpan? utcOffset) {
        IsEnabled = isEnabled;
        LocalTime = localTime;
        UtcOffset = utcOffset;
    }

    public static WeeklyGoalReminderSettings Disabled { get; } = new(isEnabled: false, localTime: null, utcOffset: null);

    public bool IsEnabled { get; }
    public TimeOnly? LocalTime { get; }
    public TimeSpan? UtcOffset { get; }
    public int? LocalTimeMinutes => LocalTime is { } time ? (time.Hour * 60) + time.Minute : null;
    public int? UtcOffsetMinutes => UtcOffset is { } offset ? (int)offset.TotalMinutes : null;

    public static WeeklyGoalReminderSettings EnabledAt(TimeOnly localTime, TimeSpan utcOffset) {
        if (utcOffset.Ticks % TimeSpan.TicksPerMinute != 0 ||
            utcOffset < TimeSpan.FromMinutes(-MaximumOffsetMinutes) ||
            utcOffset > TimeSpan.FromMinutes(MaximumOffsetMinutes)) {
            throw new ArgumentOutOfRangeException(nameof(utcOffset), "UTC offset must be whole minutes between UTC-14 and UTC+14.");
        }

        return new WeeklyGoalReminderSettings(isEnabled: true, new TimeOnly(localTime.Hour, localTime.Minute), utcOffset);
    }

    public static WeeklyGoalReminderSettings FromMinutes(bool enabled, int? timeMinutes, int? offsetMinutes) {
        if (!enabled) {
            return Disabled;
        }

        if (timeMinutes is null or < 0 or >= MinutesPerDay) {
            throw new ArgumentOutOfRangeException(nameof(timeMinutes), "Reminder time must be within a local day.");
        }

        if (offsetMinutes is null or < -MaximumOffsetMinutes or > MaximumOffsetMinutes) {
            throw new ArgumentOutOfRangeException(nameof(offsetMinutes), "Time zone offset must be between UTC-14 and UTC+14.");
        }

        return EnabledAt(new TimeOnly(timeMinutes.Value / 60, timeMinutes.Value % 60), TimeSpan.FromMinutes(offsetMinutes.Value));
    }
}
