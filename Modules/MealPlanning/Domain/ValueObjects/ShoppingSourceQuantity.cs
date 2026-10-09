using FoodDiary.Modules.Products.Domain.Contracts.Enums;

namespace FoodDiary.Modules.MealPlanning.Domain.ValueObjects;

public sealed record ShoppingSourceQuantity {
    public double Amount { get; }
    public MeasurementUnit? Unit { get; }
    private ShoppingSourceQuantity(double amount, MeasurementUnit? unit) {
        Amount = amount;
        Unit = unit;
    }

    public static ShoppingSourceQuantity FromFields(double amount, MeasurementUnit? unit) {
        if (!double.IsFinite(amount)) { throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be a finite number."); }
        if (amount <= 0) { throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be greater than zero."); }
        // Source observations retain their existing unbounded amounts and unit values.
        return new ShoppingSourceQuantity(amount, unit);
    }
}
