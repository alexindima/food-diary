using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Products.Domain.Contracts.Enums;
using FoodDiary.Domain.Primitives;

namespace FoodDiary.Modules.Recipes.Domain.Entities;

/// <summary>Transient immutable product data for recipe calculation; not an EF entity or a write capability.</summary>
public sealed record RecipeIngredientProductSnapshot(
    ProductId Id,
    string Name,
    MeasurementUnit BaseUnit,
    double BaseAmount,
    double CaloriesPerBase,
    double ProteinsPerBase,
    double FatsPerBase,
    double CarbsPerBase,
    double FiberPerBase,
    double AlcoholPerBase,
    Visibility Visibility,
    string? Category = null);
