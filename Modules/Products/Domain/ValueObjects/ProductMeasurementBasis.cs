using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Products.Domain.Contracts.Enums;
using FoodDiary.Modules.Products.Domain.Entities;

namespace FoodDiary.Modules.Products.Domain.ValueObjects;

public sealed record ProductMeasurementBasis {
    public MeasurementUnit Unit { get; }
    public double Amount { get; }

    private ProductMeasurementBasis(MeasurementUnit unit, double amount) {
        Unit = unit;
        Amount = amount;
    }

    public static ProductMeasurementBasis FromFields(MeasurementUnit unit, double amount) {
        DomainGuard.Defined(unit, nameof(unit));
        return new ProductMeasurementBasis(unit, Product.NormalizeBaseAmount(unit, amount, nameof(amount)));
    }

    public static ProductMeasurementBasis Canonical(MeasurementUnit unit) => FromFields(unit, Product.GetCanonicalBaseAmount(unit));
}
