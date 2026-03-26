using FoodDiary.Domain.Enums;

namespace FoodDiary.Domain.ValueObjects;

public readonly record struct ProductIdentityState(
    string Name,
    string? Barcode,
    string? Brand,
    string? Category,
    ProductType ProductType,
    string? Description,
    string? Comment);
