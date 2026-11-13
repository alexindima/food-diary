using FoodDiary.Modules.Tdee.Contracts.Models;

namespace FoodDiary.Modules.Tdee.Application.Services;

public union AdaptiveTdeeResult(AdaptiveTdeeResult.Calculated, AdaptiveTdeeResult.InsufficientData) {
    public sealed record Calculated(
        double AdaptiveTdee,
        TdeeConfidence Confidence,
        int DataDaysUsed,
        double WeightTrendPerWeek);

    public sealed class InsufficientData {
        internal static readonly InsufficientData Instance = new();
    }

    public double? AdaptiveTdee => this is Calculated value ? value.AdaptiveTdee : null;
    public TdeeConfidence Confidence => this is Calculated value ? value.Confidence : TdeeConfidence.None;
    public int DataDaysUsed => this is Calculated value ? value.DataDaysUsed : 0;
    public double? WeightTrendPerWeek => this is Calculated value ? value.WeightTrendPerWeek : null;
    public bool HasData => this is Calculated;
}
