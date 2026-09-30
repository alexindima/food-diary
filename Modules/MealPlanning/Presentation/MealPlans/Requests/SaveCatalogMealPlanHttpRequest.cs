using System.ComponentModel.DataAnnotations;

namespace FoodDiary.Modules.MealPlanning.Presentation.MealPlans.Requests;

public sealed record SaveCatalogMealPlanHttpRequest(
    [Required, MaxLength(256)] string Name,
    [MaxLength(2048)] string? Description,
    [Required, MaxLength(32)] string DietType,
    [Range(1, 31)] int DurationDays,
    double? TargetCaloriesPerDay,
    bool IsPublished,
    [Required, MaxLength(31)] IReadOnlyList<CatalogDayHttpRequest> Days);
