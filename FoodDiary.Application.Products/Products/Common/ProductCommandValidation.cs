using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Products.Products.Common;

internal static class ProductCommandValidation {
    public static bool BeValidUnit(string? unit) =>
        unit is not null && SharedEnumValueParser.CanParse<MeasurementUnit>(unit);

    public static bool BeValidVisibility(string? visibility) =>
        visibility is not null && SharedEnumValueParser.CanParse<Visibility>(visibility);

    public static bool BeValidProductType(string? productType) =>
        productType is not null && SharedEnumValueParser.CanParseDefined<ProductType>(productType);

    public static bool BeWithinDefaultPortionLimit(string? unit, double amount) =>
        !SharedEnumValueParser.TryParse(unit, out MeasurementUnit parsedUnit) ||
        amount <= Product.GetMaxDefaultPortionAmount(parsedUnit);

    public static bool BeWithinCaloriesLimit(string? unit, double amount) =>
        !SharedEnumValueParser.TryParse(unit, out MeasurementUnit parsedUnit) ||
        amount <= Product.GetMaxCaloriesPerBase(parsedUnit);

    public static bool BeWithinNutrientLimit(string? unit, double amount) =>
        !SharedEnumValueParser.TryParse(unit, out MeasurementUnit parsedUnit) ||
        amount <= Product.GetMaxNutrientPerBase(parsedUnit);
}
