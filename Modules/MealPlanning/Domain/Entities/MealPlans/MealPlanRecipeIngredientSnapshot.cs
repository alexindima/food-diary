using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Products.Domain.Contracts.Enums;

namespace FoodDiary.Modules.MealPlanning.Domain.Entities.MealPlans;

public sealed record MealPlanRecipeIngredientSnapshot(
    ProductId ProductId,
    double Amount,
    string Name,
    MeasurementUnit BaseUnit,
    string? Category);
