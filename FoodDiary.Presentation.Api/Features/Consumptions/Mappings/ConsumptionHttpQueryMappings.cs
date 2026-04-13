using FoodDiary.Application.Consumptions.Queries.GetConsumptions;
using FoodDiary.Application.Consumptions.Queries.GetConsumptionsOverview;
using FoodDiary.Application.Consumptions.Queries.GetConsumptionById;
using FoodDiary.Presentation.Api.Features.Consumptions.Requests;

namespace FoodDiary.Presentation.Api.Features.Consumptions.Mappings;

public static class ConsumptionHttpQueryMappings {
    public static GetConsumptionsQuery ToQuery(this GetConsumptionsHttpQuery query, Guid userId) {
        return new GetConsumptionsQuery(userId, query.Page, query.Limit, query.DateFrom, query.DateTo);
    }

    public static GetConsumptionsOverviewQuery ToQuery(this GetConsumptionsOverviewHttpQuery query, Guid userId) {
        return new GetConsumptionsOverviewQuery(
            userId,
            Math.Max(query.Page, 1),
            Math.Clamp(query.Limit, 1, 100),
            query.DateFrom,
            query.DateTo,
            Math.Clamp(query.FavoriteLimit, 1, 50));
    }

    public static GetConsumptionByIdQuery ToQuery(this Guid id, Guid userId) {
        return new GetConsumptionByIdQuery(userId, id);
    }
}
