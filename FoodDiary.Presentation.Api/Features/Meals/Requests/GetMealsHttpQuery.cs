using System.ComponentModel.DataAnnotations;
using FoodDiary.Presentation.Api.Policies;

namespace FoodDiary.Presentation.Api.Features.Meals.Requests;

public sealed record GetMealsHttpQuery(
    [OpenApiNumericRange(PresentationQueryLimits.MinimumPage, PresentationQueryLimits.MaximumPage)] int Page = 1,
    [OpenApiNumericRange(PresentationQueryLimits.MinimumPageSize, PresentationQueryLimits.MaximumPageSize)] int Limit = 10,
    DateTime? DateFrom = null,
    DateTime? DateTo = null,
    [MaxLength(PresentationQueryLimits.MaximumCsvFilterLength)] string? MealTypes = null,
    [OpenApiNumericRange(0)] double? CaloriesFrom = null,
    [OpenApiNumericRange(0)] double? CaloriesTo = null,
    bool? HasImage = null,
    bool? HasAiSession = null);
