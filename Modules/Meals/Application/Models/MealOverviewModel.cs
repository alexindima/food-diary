using FoodDiary.Modules.Meals.Service.Contracts.Models;
using FoodDiary.Modules.Favorites.Contracts.FavoriteMeals.Models;
using FoodDiary.Application.Abstractions.Common.Models;

namespace FoodDiary.Modules.Meals.Application.Models;

public sealed record MealOverviewModel(
    PagedResponse<MealModel> AllMeals,
    IReadOnlyList<MealFavoriteMealModel> FavoriteItems,
    int FavoriteTotalCount);
