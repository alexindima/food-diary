using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;

public sealed record ProductQueryFilters(
    string? Search,
    IReadOnlyCollection<ProductType>? ProductTypes = null,
    double? CaloriesFrom = null,
    double? CaloriesTo = null,
    bool? HasImage = null);
