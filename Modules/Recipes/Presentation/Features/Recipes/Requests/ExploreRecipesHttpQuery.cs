using System.ComponentModel.DataAnnotations;
using FoodDiary.Presentation.Api.Policies;

namespace FoodDiary.Presentation.Api.Features.Recipes.Requests;

public sealed record ExploreRecipesHttpQuery(
    [OpenApiNumericRange(PresentationQueryLimits.MinimumPage, PresentationQueryLimits.MaximumPage)] int Page = 1,
    [OpenApiNumericRange(PresentationQueryLimits.MinimumPageSize, PresentationQueryLimits.MaximumRecentItems)] int Limit = 20,
    [MaxLength(PresentationQueryLimits.MaximumSearchLength)] string? Search = null,
    [MaxLength(PresentationQueryLimits.MaximumCategoryLength)] string? Category = null,
    [OpenApiNumericRange(1)] int? MaxPrepTime = null,
    [Required, MaxLength(PresentationQueryLimits.MaximumSortLength)]
    [AllowedQueryValues(
        PresentationQueryValues.Newest,
        PresentationQueryValues.Popular)] string SortBy = "newest");
