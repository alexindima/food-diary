using FoodDiary.Presentation.Api.Features.FavoriteMeals.Responses;
using FoodDiary.Presentation.Api.Responses;

namespace FoodDiary.Presentation.Api.Features.Consumptions.Responses;

public sealed record ConsumptionOverviewHttpResponse(
    PagedHttpResponse<ConsumptionHttpResponse> AllConsumptions,
    IReadOnlyList<FavoriteMealHttpResponse> FavoriteItems,
    int FavoriteTotalCount);
