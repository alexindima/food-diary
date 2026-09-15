using FoodDiary.Modules.MealPlanning.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

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
