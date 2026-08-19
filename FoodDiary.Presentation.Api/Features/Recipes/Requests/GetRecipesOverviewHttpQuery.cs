using System.ComponentModel.DataAnnotations;
using FoodDiary.Presentation.Api.Policies;

namespace FoodDiary.Presentation.Api.Features.Recipes.Requests;

public sealed record GetRecipesOverviewHttpQuery(
    [OpenApiNumericRange(PresentationQueryLimits.MinimumPage, PresentationQueryLimits.MaximumPage)] int Page = 1,
    [OpenApiNumericRange(PresentationQueryLimits.MinimumPageSize, PresentationQueryLimits.MaximumPageSize)] int Limit = 10,
    [OpenApiNumericRange(PresentationQueryLimits.MinimumPageSize, PresentationQueryLimits.MaximumRecentItems)] int RecentLimit = 10,
    [OpenApiNumericRange(PresentationQueryLimits.MinimumPageSize, PresentationQueryLimits.MaximumRecentItems)] int FavoriteLimit = 10,
    [MaxLength(PresentationQueryLimits.MaximumSearchLength)] string? Search = null,
    bool IncludePublic = true,
    [MaxLength(PresentationQueryLimits.MaximumCategoryLength)] string? Category = null,
    [OpenApiNumericRange(1)] int? MaxTotalTime = null,
    [OpenApiNumericRange(0)] double? CaloriesFrom = null,
    [OpenApiNumericRange(0)] double? CaloriesTo = null,
    bool? HasImage = null);
