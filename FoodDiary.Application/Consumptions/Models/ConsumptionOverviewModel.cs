using FoodDiary.Application.Abstractions.Consumptions.Models;
using FoodDiary.Application.Abstractions.Common.Models;

namespace FoodDiary.Application.Consumptions.Models;

public sealed record ConsumptionOverviewModel(
    PagedResponse<ConsumptionModel> AllConsumptions,
    IReadOnlyList<ConsumptionFavoriteMealModel> FavoriteItems,
    int FavoriteTotalCount);
