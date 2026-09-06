using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.MealPlans;

public sealed record MealPlanRecipeIngredientSnapshot(
    ProductId ProductId,
    double Amount,
    string Name,
    MeasurementUnit BaseUnit,
    string? Category);
