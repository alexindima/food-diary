namespace FoodDiary.Domain.ValueObjects;

public readonly record struct DesiredWeight {
    public const double MaxValue = 500d;

    public double Value { get; }

    private DesiredWeight(double value) {
        Value = value;
    }

    public static DesiredWeight Create(double value) {
        if (double.IsNaN(value) || double.IsInfinity(value)) {
            throw new ArgumentOutOfRangeException(nameof(value), "Desired weight must be a finite number.");
        }

        return value is <= 0 or > MaxValue
            ? throw new ArgumentOutOfRangeException(nameof(value), $"Desired weight must be in range (0, {MaxValue}].")
            : new DesiredWeight(value);
    }
}
