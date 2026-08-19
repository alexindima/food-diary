using System.ComponentModel.DataAnnotations;
using FoodDiary.Presentation.Api.Policies;

namespace FoodDiary.Presentation.Api.Features.Recipes.Requests;

public sealed record GetRecipesHttpQuery(
    int Page = 1,
    int Limit = 10,
    [MaxLength(PresentationQueryLimits.MaximumSearchLength)] string? Search = null,
    bool IncludePublic = true,
    [MaxLength(PresentationQueryLimits.MaximumCategoryLength)] string? Category = null,
    int? MaxTotalTime = null,
    double? CaloriesFrom = null,
    double? CaloriesTo = null,
    bool? HasImage = null);
