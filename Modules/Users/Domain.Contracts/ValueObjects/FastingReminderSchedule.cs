namespace FoodDiary.Modules.Users.Domain.Contracts.ValueObjects;

/// <summary>Ordered due delays, with an explicit compatibility path for stored legacy observations.</summary>
public sealed record FastingReminderSchedule {
    public int FirstHours { get; }
    public int FollowUpHours { get; }
    public bool IsLegacy => FirstHours is < 1 or > 168 || FollowUpHours is < 1 or > 168 || FollowUpHours <= FirstHours;
    public FastingReminderSettings? Settings => IsLegacy ? null : FastingReminderSettings.FromHours(FirstHours, FollowUpHours);
    public IReadOnlyList<int> DueHours => Array.AsReadOnly(new[] { FirstHours, FollowUpHours }.Distinct().Order().ToArray());
    public static FastingReminderSchedule Default { get; } = FromSettings(FastingReminderSettings.Default);

    private FastingReminderSchedule(int firstHours, int followUpHours) {
        FirstHours = firstHours;
        FollowUpHours = followUpHours;
    }

    public static FastingReminderSchedule FromSettings(FastingReminderSettings settings) => new(settings.FirstHours, settings.FollowUpHours);

    public static FastingReminderSchedule FromStoredHours(int firstHours, int followUpHours) => new(firstHours, followUpHours);

    public FastingReminderSchedule Merge(FastingReminderDelayUpdate update) {
        FastingReminderSettings.EnsureHour(update.FirstHours, "fastingCheckInReminderHours");
        FastingReminderSettings.EnsureHour(update.FollowUpHours, "fastingCheckInFollowUpReminderHours");
        int first = update.FirstHours ?? FirstHours;
        int followUp = update.FollowUpHours ?? FollowUpHours;
        FastingReminderSettings.EnsureOrder(first, followUp);
        return FromStoredHours(first, followUp);
    }
}
