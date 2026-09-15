using FoodDiary.Modules.Favorites.Presentation.Contracts.Features.FavoriteMeals.Responses;
using FoodDiary.Presentation.Api.Responses;

namespace FoodDiary.Presentation.Api.Features.Meals.Responses;

public sealed record MealOverviewHttpResponse(
    PagedHttpResponse<MealHttpResponse> AllMeals,
    IReadOnlyList<FavoriteMealHttpResponse> FavoriteItems,
    int FavoriteTotalCount);
