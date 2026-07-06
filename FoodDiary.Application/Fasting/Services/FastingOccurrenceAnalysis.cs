using FoodDiary.Application.Abstractions.Fasting.Models;

namespace FoodDiary.Application.Fasting.Services;

internal sealed record FastingOccurrenceAnalysis(
    FastingOccurrenceReadModel Occurrence,
    IReadOnlyList<FastingCheckInSnapshot> Timeline,
    FastingCheckInSnapshot? LatestCheckIn);
