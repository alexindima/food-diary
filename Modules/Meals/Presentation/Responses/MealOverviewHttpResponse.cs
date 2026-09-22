using FoodDiary.Modules.Meals.Presentation.Contracts.Responses;
using FoodDiary.Modules.Favorites.Presentation.Contracts.Features.FavoriteMeals.Responses;
using FoodDiary.Presentation.Api.Responses;

namespace FoodDiary.Modules.Meals.Presentation.Responses;

public sealed record MealOverviewHttpResponse(
    PagedHttpResponse<MealHttpResponse> AllMeals,
    IReadOnlyList<FavoriteMealHttpResponse> FavoriteItems,
    int FavoriteTotalCount) {
    public IReadOnlyList<MealDaySummaryHttpResponse> DaySummaries { get; init; } = [];
}
