using System.ComponentModel.DataAnnotations;
using FoodDiary.Presentation.Api.Policies;

namespace FoodDiary.Presentation.Api.Features.Recipes.Requests;

public sealed record ExploreRecipesHttpQuery(
    int Page = 1,
    int Limit = 20,
    [MaxLength(PresentationQueryLimits.MaximumSearchLength)] string? Search = null,
    [MaxLength(PresentationQueryLimits.MaximumCategoryLength)] string? Category = null,
    int? MaxPrepTime = null,
    [Required, MaxLength(PresentationQueryLimits.MaximumSortLength)]
    [AllowedQueryValues(
        PresentationQueryValues.Newest,
        PresentationQueryValues.Popular)] string SortBy = "newest");
