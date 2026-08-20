using System.Globalization;

namespace FoodDiary.Domain.ValueObjects;

public readonly record struct ProfileWeightKg {
    public const double MaxValue = 500d;

    public double Value { get; }

    private ProfileWeightKg(double value) {
        Value = value;
    }

    public static ProfileWeightKg Create(double value) {
        if (!double.IsFinite(value)) {
            throw new ArgumentOutOfRangeException(nameof(value), "Profile weight must be a finite number.");
        }

        return value is <= 0 or > MaxValue
            ? throw new ArgumentOutOfRangeException(nameof(value), string.Create(CultureInfo.InvariantCulture, $"Profile weight must be in range (0, {MaxValue}]."))
            : new ProfileWeightKg(value);
    }
}
