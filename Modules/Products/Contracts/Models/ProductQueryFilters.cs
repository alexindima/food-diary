using FoodDiary.Modules.Products.Domain.Contracts.Enums;

namespace FoodDiary.Modules.Products.Contracts.Models;

public sealed record ProductQueryFilters(
    string? Search,
    IReadOnlyCollection<ProductType>? ProductTypes = null,
    double? CaloriesFrom = null,
    double? CaloriesTo = null,
    bool? HasImage = null);
