using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects;

namespace FoodDiary.Modules.Tdee.Application.Common;

public sealed record CalculationMeasuredWeight {
    public double Value { get; }
    public MeasuredWeightKg? Validated { get; }
    private CalculationMeasuredWeight(double value, MeasuredWeightKg? validated) {
        Value = value;
        Validated = validated;
    }

    public static CalculationMeasuredWeight FromMeasurement(MeasuredWeightKg value) {
        ArgumentNullException.ThrowIfNull(value);
        return new CalculationMeasuredWeight(value.Value, value);
    }

    public static CalculationMeasuredWeight? FromOptionalStoredValue(double? value) {
        if (!value.HasValue) {
            return null;
        }
        return double.IsFinite(value.Value) && value.Value is > 0 and <= MeasuredWeightKg.MaxValue
            ? FromMeasurement(MeasuredWeightKg.Create(value.Value))
            : new CalculationMeasuredWeight(value.Value, validated: null);
    }
}
