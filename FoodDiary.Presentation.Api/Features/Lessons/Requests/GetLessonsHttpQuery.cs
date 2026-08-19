using System.ComponentModel.DataAnnotations;
using FoodDiary.Presentation.Api.Policies;

namespace FoodDiary.Presentation.Api.Features.Lessons.Requests;

public sealed record GetLessonsHttpQuery(
    [Required, MaxLength(PresentationQueryLimits.MaximumLocaleLength)] string Locale = "en",
    [MaxLength(PresentationQueryLimits.MaximumCategoryLength)]
    [AllowedQueryValues(
        PresentationQueryValues.NutritionBasics,
        PresentationQueryValues.Macronutrients,
        PresentationQueryValues.Micronutrients,
        PresentationQueryValues.MealTiming,
        PresentationQueryValues.MindfulEating,
        PresentationQueryValues.WeightManagement,
        PresentationQueryValues.Hydration,
        PresentationQueryValues.FoodQuality,
        PresentationQueryValues.CookingTips)] string? Category = null,
    [MaxLength(PresentationQueryLimits.MaximumFilterLength)]
    [AllowedQueryValues(
        PresentationQueryValues.Beginner,
        PresentationQueryValues.Intermediate,
        PresentationQueryValues.Advanced)] string? Difficulty = null,
    [MaxLength(PresentationQueryLimits.MaximumSearchLength)] string? Search = null,
    [MaxLength(PresentationQueryLimits.MaximumSortLength)]
    [AllowedQueryValues(
        PresentationQueryValues.Recommended,
        PresentationQueryValues.Shortest)] string? Sort = null,
    int Page = 1,
    int PageSize = 20);
