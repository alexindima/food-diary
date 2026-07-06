using FoodDiary.Application.Abstractions.Fasting.Models;
using FoodDiary.Application.Fasting.Models;

namespace FoodDiary.Application.Fasting.Services;

internal static class FastingInsightBuilder {
    public static IReadOnlyList<FastingMessageModel> BuildInsights(IReadOnlyList<FastingOccurrenceAnalysis> analyses) =>
        FastingHistoricalInsightBuilder.Build(analyses);

    public static IReadOnlyList<FastingMessageModel> BuildAlerts(
        FastingOccurrenceReadModel? current,
        FastingCheckInSnapshot? latestCheckIn,
        DateTime nowUtc) =>
        FastingAlertBuilder.Build(current, latestCheckIn, nowUtc);
}
