using FoodDiary.Application.Consumptions.Queries.GetConsumptions;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Presentation.Api.Features.Consumptions.Requests;

namespace FoodDiary.Presentation.Api.Features.Consumptions.Mappings;

public static class ConsumptionHttpQueryMappings {
    public static GetConsumptionsQuery ToQuery(this GetConsumptionsHttpQuery query, UserId userId) {
        return new GetConsumptionsQuery(userId, query.Page, query.Limit, query.DateFrom, query.DateTo);
    }
}
