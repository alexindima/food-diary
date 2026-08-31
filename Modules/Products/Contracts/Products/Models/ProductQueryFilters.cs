using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Abstractions.Products.Models;

public sealed record ProductQueryFilters(
    string? Search,
    IReadOnlyCollection<ProductType>? ProductTypes = null,
    double? CaloriesFrom = null,
    double? CaloriesTo = null,
    bool? HasImage = null);
