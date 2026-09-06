using FoodDiary.Domain.Entities.Tracking.Fasting;

namespace FoodDiary.Application.Abstractions.Fasting.Models;

public sealed record FastingActiveOccurrenceModel(
    FastingOccurrence Occurrence,
    int ReminderHours,
    int FollowUpReminderHours);
