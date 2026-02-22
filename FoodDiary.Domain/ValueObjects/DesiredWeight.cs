namespace FoodDiary.Domain.ValueObjects;

public readonly record struct DesiredWeight {
    public const double MaxValue = 500d;

    public double Value { get; }

    private DesiredWeight(double value) {
        Value = value;
    }

    public static DesiredWeight Create(double value) {
        return value is <= 0 or > MaxValue
            ? throw new ArgumentOutOfRangeException(nameof(value), $"Desired weight must be in range (0, {MaxValue}].")
            : new DesiredWeight(value);
    }
}
