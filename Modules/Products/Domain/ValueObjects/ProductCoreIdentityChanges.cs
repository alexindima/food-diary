using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Products.Domain.Contracts.Enums;

namespace FoodDiary.Modules.Products.Domain.ValueObjects;

public sealed record ProductCoreIdentityChanges(
    string? Name,
    FieldChange<string> Barcode,
    FieldChange<string> Brand,
    ProductType? ProductType);
