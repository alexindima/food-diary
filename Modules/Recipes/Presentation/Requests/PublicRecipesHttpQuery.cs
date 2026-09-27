using System.ComponentModel.DataAnnotations;
using FoodDiary.Presentation.Api.Policies;

namespace FoodDiary.Modules.Recipes.Presentation.Requests;

public sealed record PublicRecipesHttpQuery(
    [OpenApiNumericRange(PresentationQueryLimits.MinimumPage, PresentationQueryLimits.MaximumPage)] int Page = 1,
    [OpenApiNumericRange(PresentationQueryLimits.MinimumPageSize, PresentationQueryLimits.MaximumRecentItems)] int Limit = 20,
    [MaxLength(PresentationQueryLimits.MaximumSearchLength)] string? Search = null,
    [MaxLength(PresentationQueryLimits.MaximumCategoryLength)] string? Category = null,
    [OpenApiNumericRange(1)] int? MaxTotalTime = null,
    [RegularExpression("^(newest|oldest|fastest|slowest|name|name_desc)$")] string SortBy = "newest",
    [RegularExpression("^(en|ru)$")] string? Language = null);
