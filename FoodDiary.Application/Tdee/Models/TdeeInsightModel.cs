namespace FoodDiary.Application.Tdee.Models;

public sealed record TdeeInsightModel(
    double? EstimatedTdee,
    double? AdaptiveTdee,
    double? Bmr,
    double? SuggestedCalorieTarget,
    double? CurrentCalorieTarget,
    double? WeightTrendPerWeek,
    TdeeConfidence Confidence,
    int DataDaysUsed,
    string? GoalAdjustmentHint);

public enum TdeeConfidence {
    None = 0,
    Low = 1,
    Medium = 2,
    High = 3
}
