using FoodDiary.Domain.Entities.Tracking.Fasting;

namespace FoodDiary.Application.Fasting.Services;

internal sealed record FastingOccurrenceAnalysis(
    FastingOccurrence Occurrence,
    IReadOnlyList<FastingCheckInSnapshot> Timeline,
    FastingCheckInSnapshot? LatestCheckIn);
