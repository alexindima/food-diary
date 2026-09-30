using System.ComponentModel.DataAnnotations;

namespace FoodDiary.Modules.MealPlanning.Presentation.MealPlans.Requests;

public sealed record CatalogMealHttpRequest([Required, MaxLength(32)] string MealType, Guid RecipeId,
    [Range(1, 100)] int Servings);
