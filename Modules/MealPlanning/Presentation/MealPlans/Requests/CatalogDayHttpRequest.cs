using System.ComponentModel.DataAnnotations;

namespace FoodDiary.Modules.MealPlanning.Presentation.MealPlans.Requests;

public sealed record CatalogDayHttpRequest([Range(1, 31)] int DayNumber,
    [Required, MaxLength(20)] IReadOnlyList<CatalogMealHttpRequest> Meals);
