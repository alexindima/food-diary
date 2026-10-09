namespace FoodDiary.Modules.Users.Domain.Contracts.ValueObjects;

public sealed record FastingReminderSettings {
    public int FirstHours { get; }
    public int FollowUpHours { get; }
    public static FastingReminderSettings Default { get; } = new(12, 20);

    private FastingReminderSettings(int firstHours, int followUpHours) {
        FirstHours = firstHours;
        FollowUpHours = followUpHours;
    }

    public static FastingReminderSettings FromHours(int firstHours, int followUpHours) {
        EnsureHour(firstHours, "fastingCheckInReminderHours");
        EnsureHour(followUpHours, "fastingCheckInFollowUpReminderHours");
        EnsureOrder(firstHours, followUpHours);
        return new FastingReminderSettings(firstHours, followUpHours);
    }

    public static void EnsureHour(int? value, string parameterName) {
        if (value is < 1 or > 168) {
            throw new ArgumentOutOfRangeException(parameterName, "Reminder hour must be between 1 and 168.");
        }
    }

    internal static void EnsureOrder(int fastingCheckInReminderHours, int fastingCheckInFollowUpReminderHours) {
        if (fastingCheckInFollowUpReminderHours <= fastingCheckInReminderHours) {
            throw new ArgumentOutOfRangeException(nameof(fastingCheckInFollowUpReminderHours),
                "Follow-up reminder hour must be greater than the first reminder hour.");
        }
    }
}
