using FoodDiary.Application.Common.Models;

namespace FoodDiary.Application.Consumptions.Models;

public sealed record ConsumptionOverviewModel(
    PagedResponse<ConsumptionModel> AllConsumptions,
    IReadOnlyList<ConsumptionFavoriteMealModel> FavoriteItems,
    int FavoriteTotalCount);
