namespace FoodDiary.Modules.MealPlanning.Application.MealPlans.Commands.SaveCatalogMealPlan;

public sealed record CatalogDayInput(int DayNumber, IReadOnlyList<CatalogMealInput> Meals);
