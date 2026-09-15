using FoodDiary.Modules.Products.Domain.Contracts.Enums;

namespace FoodDiary.Modules.Products.Domain.ValueObjects;

public readonly record struct ProductIdentityState(
    string Name,
    string? Barcode,
    string? Brand,
    string? Category,
    ProductType ProductType,
    string? Description,
    string? Comment);
