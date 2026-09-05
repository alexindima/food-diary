namespace FoodDiary.Presentation.Api.Features.Tdee.Responses;

public sealed record TdeeInsightHttpResponse(
    double? EstimatedTdee,
    double? AdaptiveTdee,
    double? Bmr,
    double? SuggestedCalorieTarget,
    double? CurrentCalorieTarget,
    double? WeightTrendPerWeek,
    string Confidence,
    int DataDaysUsed,
    string? GoalAdjustmentHint);
