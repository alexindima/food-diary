using FoodDiary.Modules.Fasting.Domain.Entities.Tracking.Fasting;

namespace FoodDiary.Modules.Fasting.Application.Abstractions.Models;

public sealed record FastingActiveOccurrenceModel(
    FastingOccurrence Occurrence,
    int ReminderHours,
    int FollowUpReminderHours);
