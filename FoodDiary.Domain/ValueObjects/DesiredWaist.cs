namespace FoodDiary.Domain.ValueObjects;

public readonly record struct DesiredWaist {
    public const double MaxValue = 300d;

    public double Value { get; }

    private DesiredWaist(double value) {
        Value = value;
    }

    public static DesiredWaist Create(double value) {
        if (double.IsNaN(value) || double.IsInfinity(value)) {
            throw new ArgumentOutOfRangeException(nameof(value), "Desired waist must be a finite number.");
        }

        return value is <= 0 or > MaxValue
            ? throw new ArgumentOutOfRangeException(nameof(value), $"Desired waist must be in range (0, {MaxValue}].")
            : new DesiredWaist(value);
    }
}
