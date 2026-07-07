using FoodDiary.Application.Common.Validation;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Products.Common;

internal static class ProductCommandValidation {
    public static bool BeValidUnit(string? unit) =>
        unit is not null && EnumValueParser.CanParse<MeasurementUnit>(unit);

    public static bool BeValidVisibility(string? visibility) =>
        visibility is not null && EnumValueParser.CanParse<Visibility>(visibility);

    public static bool BeValidProductType(string? productType) =>
        productType is not null && EnumValueParser.CanParseDefined<ProductType>(productType);

    public static bool BeWithinDefaultPortionLimit(string? unit, double amount) =>
        !EnumValueParser.TryParse(unit, out MeasurementUnit parsedUnit) ||
        amount <= Product.GetMaxDefaultPortionAmount(parsedUnit);

    public static bool BeWithinCaloriesLimit(string? unit, double amount) =>
        !EnumValueParser.TryParse(unit, out MeasurementUnit parsedUnit) ||
        amount <= Product.GetMaxCaloriesPerBase(parsedUnit);

    public static bool BeWithinNutrientLimit(string? unit, double amount) =>
        !EnumValueParser.TryParse(unit, out MeasurementUnit parsedUnit) ||
        amount <= Product.GetMaxNutrientPerBase(parsedUnit);
}
