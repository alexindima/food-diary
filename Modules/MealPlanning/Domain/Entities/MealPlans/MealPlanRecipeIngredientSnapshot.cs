using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.MealPlanning.Domain.Entities.MealPlans;

public sealed record MealPlanRecipeIngredientSnapshot(
    ProductId ProductId,
    double Amount,
    string Name,
    MeasurementUnit BaseUnit,
    string? Category);
