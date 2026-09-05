using FoodDiary.Application.Hydration.Models;
using FoodDiary.Presentation.Api.Features.Hydration.Responses;

namespace FoodDiary.Presentation.Api.Features.Hydration.Mappings;

public static class HydrationHttpResponseMappings {
    extension(HydrationEntryModel model) {
        public HydrationEntryHttpResponse ToHttpResponse() {
            return new HydrationEntryHttpResponse(model.Id, model.TimestampUtc, model.AmountMl);
        }
    }

    extension(HydrationDailyModel model) {
        public HydrationDailyHttpResponse ToHttpResponse() {
            return new HydrationDailyHttpResponse(model.DateUtc, model.TotalMl, model.GoalMl);
        }
    }
}
