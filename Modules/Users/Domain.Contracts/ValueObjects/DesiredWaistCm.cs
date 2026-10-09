using System.Globalization;

namespace FoodDiary.Modules.Users.Domain.Contracts.ValueObjects;

public sealed record DesiredWaistCm {
    public const double MaxValue = 300d;

    public double Value { get; }

    private DesiredWaistCm(double value) {
        Value = value;
    }

    public static DesiredWaistCm? FromOptional(double? value) => value.HasValue ? Create(value.Value) : null;

    public static DesiredWaistCm Create(double value) {
        if (double.IsNaN(value) || double.IsInfinity(value)) {
            throw new ArgumentOutOfRangeException(nameof(value), "Desired waist must be a finite number.");
        }

        return value is <= 0 or > MaxValue
            ? throw new ArgumentOutOfRangeException(nameof(value), string.Create(CultureInfo.InvariantCulture, $"Desired waist must be in range (0, {MaxValue}]."))
            : new DesiredWaistCm(value);
    }
}
