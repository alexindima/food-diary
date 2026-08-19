using System.ComponentModel.DataAnnotations;
using FoodDiary.Presentation.Api.Policies;

namespace FoodDiary.Presentation.Api.Features.Meals.Requests;

public sealed record GetMealsHttpQuery(
    int Page = 1,
    int Limit = 10,
    DateTime? DateFrom = null,
    DateTime? DateTo = null,
    [MaxLength(PresentationQueryLimits.MaximumCsvFilterLength)] string? MealTypes = null,
    double? CaloriesFrom = null,
    double? CaloriesTo = null,
    bool? HasImage = null,
    bool? HasAiSession = null);
