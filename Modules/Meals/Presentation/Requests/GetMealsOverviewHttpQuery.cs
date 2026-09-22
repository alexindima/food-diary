using System.ComponentModel.DataAnnotations;
using FoodDiary.Presentation.Api.Policies;

namespace FoodDiary.Modules.Meals.Presentation.Requests;

public sealed record GetMealsOverviewHttpQuery(
    [OpenApiNumericRange(PresentationQueryLimits.MinimumPage, PresentationQueryLimits.MaximumPage)] int Page = 1,
    [OpenApiNumericRange(PresentationQueryLimits.MinimumPageSize, PresentationQueryLimits.MaximumPageSize)] int Limit = 10,
    DateTime? DateFrom = null,
    DateTime? DateTo = null,
    [OpenApiNumericRange(0, PresentationQueryLimits.MaximumRecentItems)] int FavoriteLimit = 10,
    [MaxLength(PresentationQueryLimits.MaximumCsvFilterLength)] string? MealTypes = null,
    [OpenApiNumericRange(0)] double? CaloriesFrom = null,
    [OpenApiNumericRange(0)] double? CaloriesTo = null,
    bool? HasImage = null,
    bool? HasAiSession = null,
    [MaxLength(100)] string? TimeZoneId = null,
    [OpenApiNumericRange(PresentationQueryLimits.MinimumTimeZoneOffsetMinutes, PresentationQueryLimits.MaximumTimeZoneOffsetMinutes)] int? TimeZoneOffsetMinutes = null,
    bool IncludeFavorites = true);
