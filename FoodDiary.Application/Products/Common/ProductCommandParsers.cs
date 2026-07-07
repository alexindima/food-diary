using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Products.Common;

internal static class ProductCommandParsers {
    public static Result<MeasurementUnit> ParseRequiredBaseUnit(string value, string propertyName) =>
        EnumValueParser.ParseRequired<MeasurementUnit>(
            value,
            propertyName,
            "Unknown measurement unit value.");

    public static Result<MeasurementUnit?> ParseOptionalBaseUnit(string? value, string propertyName) =>
        ParseOptionalEnum<MeasurementUnit>(
            value,
            propertyName,
            "Unknown measurement unit value.");

    public static Result<Visibility> ParseRequiredVisibility(string value, string propertyName) =>
        EnumValueParser.ParseRequired<Visibility>(
            value,
            propertyName,
            "Unknown visibility value.");

    public static Result<Visibility?> ParseOptionalVisibility(string? value, string propertyName) =>
        ParseOptionalEnum<Visibility>(
            value,
            propertyName,
            "Unknown visibility value.");

    public static Result<ProductType> ParseRequiredProductType(string value, string propertyName) {
        Result<ProductType> productTypeResult = EnumValueParser.ParseRequired<ProductType>(
            value,
            propertyName,
            "Unknown product type value.");
        if (productTypeResult.IsFailure) {
            return productTypeResult;
        }

        return Enum.IsDefined(productTypeResult.Value)
            ? productTypeResult
            : Result.Failure<ProductType>(
                Errors.Validation.Invalid(propertyName, "Unknown product type value."));
    }

    public static Result<ProductType?> ParseOptionalProductType(string? value, string propertyName) {
        Result<ProductType?> productTypeResult = ParseOptionalEnum<ProductType>(
            value,
            propertyName,
            "Unknown product type value.");
        if (productTypeResult.IsFailure) {
            return productTypeResult;
        }

        return productTypeResult.Value.HasValue && !Enum.IsDefined(productTypeResult.Value.Value)
            ? Result.Failure<ProductType?>(
                Errors.Validation.Invalid(propertyName, "Unknown product type value."))
            : productTypeResult;
    }

    private static Result<TEnum?> ParseOptionalEnum<TEnum>(
        string? value,
        string propertyName,
        string errorMessage)
        where TEnum : struct, Enum =>
        string.IsNullOrWhiteSpace(value)
            ? Result.Success<TEnum?>(value: null)
            : EnumValueParser.ParseOptional<TEnum>(value, propertyName, errorMessage);
}
