using FoodDiary.Application.Tdee.Models;

namespace FoodDiary.Application.Tdee.Services;

public sealed record AdaptiveTdeeResult(
    double? AdaptiveTdee,
    TdeeConfidence Confidence,
    int DataDaysUsed,
    double? WeightTrendPerWeek) {
    public static readonly AdaptiveTdeeResult Insufficient = new(null, TdeeConfidence.None, 0, null);

    public bool HasData => AdaptiveTdee.HasValue;
}
