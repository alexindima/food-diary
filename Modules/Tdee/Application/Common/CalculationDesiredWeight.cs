using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects;

namespace FoodDiary.Modules.Tdee.Application.Common;

public sealed record CalculationDesiredWeight {
    public double Value { get; }
    public DesiredWeightKg? Validated { get; }
    private CalculationDesiredWeight(double value, DesiredWeightKg? validated) {
        Value = value;
        Validated = validated;
    }

    public static CalculationDesiredWeight FromMeasurement(DesiredWeightKg value) {
        ArgumentNullException.ThrowIfNull(value);
        return new CalculationDesiredWeight(value.Value, value);
    }

    public static CalculationDesiredWeight? FromOptionalStoredValue(double? value) {
        if (!value.HasValue) {
            return null;
        }
        return double.IsFinite(value.Value) && value.Value is > 0 and <= DesiredWeightKg.MaxValue
            ? FromMeasurement(DesiredWeightKg.Create(value.Value))
            : new CalculationDesiredWeight(value.Value, validated: null);
    }
}
