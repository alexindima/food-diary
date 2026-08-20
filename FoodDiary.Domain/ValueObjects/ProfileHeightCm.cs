using System.Globalization;

namespace FoodDiary.Domain.ValueObjects;

public readonly record struct ProfileHeightCm {
    public const double MaxValue = 300d;

    public double Value { get; }

    private ProfileHeightCm(double value) {
        Value = value;
    }

    public static ProfileHeightCm Create(double value) {
        if (!double.IsFinite(value)) {
            throw new ArgumentOutOfRangeException(nameof(value), "Profile height must be a finite number.");
        }

        return value is <= 0 or > MaxValue
            ? throw new ArgumentOutOfRangeException(nameof(value), string.Create(CultureInfo.InvariantCulture, $"Profile height must be in range (0, {MaxValue}]."))
            : new ProfileHeightCm(value);
    }
}
