using FoodDiary.Application.Hydration.Models;
using FoodDiary.Presentation.Api.Features.Hydration.Responses;

namespace FoodDiary.Presentation.Api.Features.Hydration.Mappings;

public static class HydrationHttpResponseMappings {
    public static HydrationEntryHttpResponse ToHttpResponse(this HydrationEntryModel model) {
        return new HydrationEntryHttpResponse(model.Id, model.TimestampUtc, model.AmountMl);
    }

    public static HydrationDailyHttpResponse ToHttpResponse(this HydrationDailyModel model) {
        return new HydrationDailyHttpResponse(model.DateUtc, model.TotalMl, model.GoalMl);
    }
}
