namespace FoodDiary.Modules.MealPlanning.Application.Abstractions.MealPlans.Models;

public sealed record CatalogRecipeReadModel(Guid Id, string Name, int Servings);
