using FoodDiary.Modules.Meals.Application.Abstractions.Models;
using FoodDiary.Modules.Meals.Service.Contracts.Models;
using FoodDiary.Modules.Favorites.Contracts.FavoriteMeals.Models;
using FoodDiary.Application.Contracts.Common.Models;

namespace FoodDiary.Modules.Meals.Application.Models;

public sealed record MealOverviewModel(
    PagedResponse<MealModel> AllMeals,
    IReadOnlyList<MealFavoriteMealModel> FavoriteItems,
    int FavoriteTotalCount) {
    public IReadOnlyList<MealDaySummary> DaySummaries { get; init; } = [];
}
