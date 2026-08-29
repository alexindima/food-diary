
namespace FoodDiary.Modules.Fasting.Application.Services;

internal sealed record FastingOccurrenceAnalysis(
    FastingOccurrenceReadModel Occurrence,
    IReadOnlyList<FastingCheckInSnapshot> Timeline,
    FastingCheckInSnapshot? LatestCheckIn);
