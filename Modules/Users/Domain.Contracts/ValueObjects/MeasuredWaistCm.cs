using System.Globalization;

namespace FoodDiary.Modules.Users.Domain.Contracts.ValueObjects;

public sealed record MeasuredWaistCm {
    public const double MaxValue = DesiredWaistCm.MaxValue;
    public double Value { get; }

    private MeasuredWaistCm(double value) => Value = value;

    public static MeasuredWaistCm FromGoalValue(double value) {
        _ = DesiredWaistCm.Create(value);
        return new MeasuredWaistCm(value);
    }

    public static MeasuredWaistCm Create(double value) {
        if (!double.IsFinite(value)) {
            throw new ArgumentOutOfRangeException(nameof(value), "CircumferenceCm must be a finite number.");
        }
        return value is <= 0 or > MaxValue
            ? throw new ArgumentOutOfRangeException(nameof(value), string.Create(CultureInfo.InvariantCulture, $"CircumferenceCm must be in range (0, {MaxValue}]."))
            : new MeasuredWaistCm(value);
    }
}
