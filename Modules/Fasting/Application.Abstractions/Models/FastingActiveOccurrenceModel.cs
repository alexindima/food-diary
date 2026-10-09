using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects;
using FoodDiary.Modules.Fasting.Domain.Entities.Tracking.Fasting;

namespace FoodDiary.Modules.Fasting.Application.Abstractions.Models;

public sealed record FastingActiveOccurrenceModel(
    FastingOccurrence Occurrence,
    FastingReminderSchedule ReminderDelays) {
    public int ReminderHours => ReminderDelays.FirstHours;
    public int FollowUpReminderHours => ReminderDelays.FollowUpHours;
    public FastingActiveOccurrenceModel(FastingOccurrence occurrence, int reminderHours, int followUpReminderHours)
        : this(occurrence, FastingReminderSchedule.FromStoredHours(reminderHours, followUpReminderHours)) { }
}
