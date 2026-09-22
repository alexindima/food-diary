using System.ComponentModel.DataAnnotations;
using FoodDiary.Presentation.Api.Policies;

namespace FoodDiary.Modules.Favorites.Presentation.Features.FavoriteMeals.Requests;

public sealed record GetFavoriteMealPageHttpQuery(
    [OpenApiNumericRange(PresentationQueryLimits.MinimumPage, PresentationQueryLimits.MaximumPage)] int Page = 1,
    [OpenApiNumericRange(PresentationQueryLimits.MinimumPageSize, PresentationQueryLimits.MaximumPageSize)] int Limit = 10,
    [MaxLength(200)] string? Search = null);
