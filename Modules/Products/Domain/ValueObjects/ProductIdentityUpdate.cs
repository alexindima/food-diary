using FoodDiary.Domain.Enums;

namespace FoodDiary.Domain.ValueObjects;

public readonly record struct ProductIdentityUpdate(
    string? Name = null,
    string? Barcode = null,
    bool ClearBarcode = false,
    string? Brand = null,
    bool ClearBrand = false,
    string? Category = null,
    bool ClearCategory = false,
    ProductType? ProductType = null,
    string? Description = null,
    bool ClearDescription = false,
    string? Comment = null,
    bool ClearComment = false);
