using System.Globalization;
using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Products.Domain.Contracts.Enums;

namespace FoodDiary.Modules.MealPlanning.Domain.ValueObjects;

public sealed record ShoppingQuantity {
    public const double MaximumAmount = 1_000_000d;
    public double? Amount { get; }
    public MeasurementUnit? Unit { get; }
    private ShoppingQuantity(double? amount, MeasurementUnit? unit) {
        Amount = amount;
        Unit = unit;
    }

    public static ShoppingQuantity FromFields(double? amount, MeasurementUnit? unit) {
        if (unit.HasValue) { DomainGuard.Defined(unit.Value, nameof(unit)); }
        if (amount.HasValue && !double.IsFinite(amount.Value)) {
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be a finite number.");
        }
        if (amount is <= 0 or > MaximumAmount) {
            throw new ArgumentOutOfRangeException(nameof(amount), string.Create(CultureInfo.InvariantCulture, $"Amount must be in range (0, {MaximumAmount}]."));
        }
        return new ShoppingQuantity(amount, unit);
    }
}
