using FoodDiary.Application.Abstractions.Meals.Models;
using FoodDiary.Application.Abstractions.Common.Models;

namespace FoodDiary.Application.Meals.Models;

public sealed record MealOverviewModel(
    PagedResponse<MealModel> AllMeals,
    IReadOnlyList<MealFavoriteMealModel> FavoriteItems,
    int FavoriteTotalCount);
