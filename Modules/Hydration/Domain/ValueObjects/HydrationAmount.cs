namespace FoodDiary.Modules.Hydration.Domain.ValueObjects;

/// <summary>A validated quantity for one hydration entry, measured in milliliters.</summary>
public sealed record HydrationAmount {
    public const int MaximumMilliliters = 10000;

    public int Milliliters { get; }

    private HydrationAmount(int milliliters) {
        Milliliters = milliliters;
    }

    public static HydrationAmount FromMilliliters(int value) {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, MaximumMilliliters);
        return new HydrationAmount(value);
    }
}
