using FoodDiary.Domain.Enums;
using FoodDiary.Domain.Primitives;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Recipes;

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
