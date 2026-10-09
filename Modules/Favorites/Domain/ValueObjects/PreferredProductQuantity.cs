namespace FoodDiary.Modules.Favorites.Domain.ValueObjects;

public sealed record PreferredProductQuantity {
    public double Value { get; }
    private PreferredProductQuantity(double value) => Value = value;

    public static PreferredProductQuantity FromAmount(double value) {
        if (!double.IsFinite(value) || value <= 0) {
            throw new ArgumentOutOfRangeException(nameof(value), "Preferred portion amount must be a positive finite number.");
        }
        return new PreferredProductQuantity(value);
    }
}
