using FoodDiary.Modules.Tdee.Contracts.Models;

namespace FoodDiary.Modules.Tdee.Application.Services;

public sealed record AdaptiveTdeeResult(
    double? AdaptiveTdee,
    TdeeConfidence Confidence,
    int DataDaysUsed,
    double? WeightTrendPerWeek) {
    public static readonly AdaptiveTdeeResult Insufficient = new(AdaptiveTdee: null, TdeeConfidence.None, 0, WeightTrendPerWeek: null);

    public bool HasData => AdaptiveTdee.HasValue;
}
