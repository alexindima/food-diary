using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects;
namespace FoodDiary.Modules.Users.Contracts.Models;

public sealed record UserFastingReminderModel(FastingReminderSchedule ReminderDelays) {
    public int ReminderHours => ReminderDelays.FirstHours;
    public int FollowUpReminderHours => ReminderDelays.FollowUpHours;
    public UserFastingReminderModel(int reminderHours, int followUpReminderHours)
        : this(FastingReminderSchedule.FromStoredHours(reminderHours, followUpReminderHours)) { }
}
