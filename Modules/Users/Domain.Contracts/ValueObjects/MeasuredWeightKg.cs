using System.Globalization;

namespace FoodDiary.Modules.Users.Domain.Contracts.ValueObjects;

public sealed record MeasuredWeightKg {
    public const double MaxValue = DesiredWeightKg.MaxValue;
    public double Value { get; }

    private MeasuredWeightKg(double value) => Value = value;

    public static MeasuredWeightKg FromGoalValue(double value) {
        _ = DesiredWeightKg.Create(value);
        return new MeasuredWeightKg(value);
    }

    public static MeasuredWeightKg Create(double value) {
        if (!double.IsFinite(value)) {
            throw new ArgumentOutOfRangeException(nameof(value), "WeightKg must be a finite number.");
        }
        return value is <= 0 or > MaxValue
            ? throw new ArgumentOutOfRangeException(nameof(value), string.Create(CultureInfo.InvariantCulture, $"WeightKg must be in range (0, {MaxValue}]."))
            : new MeasuredWeightKg(value);
    }
}
