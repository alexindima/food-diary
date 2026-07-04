using FoodDiary.Application.Fasting.Models;
using FoodDiary.Domain.Entities.Tracking.Fasting;

namespace FoodDiary.Application.Fasting.Services;

internal static class FastingInsightBuilder {
    public static IReadOnlyList<FastingMessageModel> BuildInsights(IReadOnlyList<FastingOccurrenceAnalysis> analyses) =>
        FastingHistoricalInsightBuilder.Build(analyses);

    public static IReadOnlyList<FastingMessageModel> BuildAlerts(
        FastingOccurrence? current,
        FastingCheckInSnapshot? latestCheckIn,
        DateTime nowUtc) =>
        FastingAlertBuilder.Build(current, latestCheckIn, nowUtc);
}
