using System.Globalization;

namespace FoodDiary.Domain.ValueObjects;

public readonly record struct DesiredWeightKg {
    public const double MaxValue = 500d;

    public double Value { get; }

    private DesiredWeightKg(double value) {
        Value = value;
    }

    public static DesiredWeightKg Create(double value) {
        if (double.IsNaN(value) || double.IsInfinity(value)) {
            throw new ArgumentOutOfRangeException(nameof(value), "Desired weight must be a finite number.");
        }

        return value is <= 0 or > MaxValue
            ? throw new ArgumentOutOfRangeException(nameof(value), string.Create(CultureInfo.InvariantCulture, $"Desired weight must be in range (0, {MaxValue}]."))
            : new DesiredWeightKg(value);
    }
}
