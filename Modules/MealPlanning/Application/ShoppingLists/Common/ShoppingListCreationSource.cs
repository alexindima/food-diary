using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Products.Domain.Contracts.Enums;
using FoodDiary.Modules.MealPlanning.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Common;

public sealed record ShoppingListCreationSource(
    MealPlanId MealPlanId,
    MealPlanMealId MealPlanMealId,
    RecipeId RecipeId,
    string Label,
    int DayNumber,
    string MealType,
    double Amount,
    MeasurementUnit? Unit);
