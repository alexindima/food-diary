namespace FoodDiary.Modules.MealPlanning.Application.MealPlans.Commands.SaveCatalogMealPlan;

public sealed record CatalogMealInput(string MealType, Guid RecipeId, int Servings);
