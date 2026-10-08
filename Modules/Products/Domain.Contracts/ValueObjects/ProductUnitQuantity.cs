using System.Globalization;

namespace FoodDiary.Modules.Products.Domain.Contracts.ValueObjects;

public sealed record ProductUnitQuantity {
    public const double MaxValue = 1_000_000d;

    private ProductUnitQuantity(double value) => Value = value;

    public double Value { get; }

    public static ProductUnitQuantity FromUnits(double amount) {
        if (!double.IsFinite(amount)) {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be a finite number.");
        }

        return amount is <= 0 or > MaxValue
            ? throw new ArgumentOutOfRangeException(nameof(amount), string.Create(CultureInfo.InvariantCulture, $"Amount must be in range (0, {MaxValue}]."))
            : new ProductUnitQuantity(amount);
    }
}
