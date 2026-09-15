using FoodDiary.Modules.Hydration.Contracts.Models;
using FoodDiary.Modules.Hydration.Presentation.Contracts.Responses;

namespace FoodDiary.Modules.Hydration.Presentation.Mappings.Mappings;

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
