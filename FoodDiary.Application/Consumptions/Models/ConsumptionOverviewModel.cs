using FoodDiary.Application.Common.Models;
using FoodDiary.Application.FavoriteMeals.Models;

namespace FoodDiary.Application.Consumptions.Models;

public sealed record ConsumptionOverviewModel(
    PagedResponse<ConsumptionModel> AllConsumptions,
    IReadOnlyList<FavoriteMealModel> FavoriteItems,
    int FavoriteTotalCount);
