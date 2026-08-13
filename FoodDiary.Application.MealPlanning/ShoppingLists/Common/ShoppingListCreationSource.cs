using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.ShoppingLists.Common;

public sealed record ShoppingListCreationSource(
    MealPlanId MealPlanId,
    MealPlanMealId MealPlanMealId,
    RecipeId RecipeId,
    string Label,
    int DayNumber,
    string MealType,
    double Amount,
    MeasurementUnit? Unit);
