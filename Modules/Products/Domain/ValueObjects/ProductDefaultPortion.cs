using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Products.Domain.Contracts.Enums;
using FoodDiary.Modules.Products.Domain.Entities;

namespace FoodDiary.Modules.Products.Domain.ValueObjects;

public sealed record ProductDefaultPortion {
    public MeasurementUnit Unit { get; }
    public double Amount { get; }

    private ProductDefaultPortion(MeasurementUnit unit, double amount) {
        Unit = unit;
        Amount = amount;
    }

    public static ProductDefaultPortion FromAmount(MeasurementUnit unit, double amount) {
        DomainGuard.Defined(unit, nameof(unit));
        return new ProductDefaultPortion(unit, Product.NormalizeDefaultPortionAmount(unit, amount, nameof(amount)));
    }
}
