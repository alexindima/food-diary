using FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Models;
using FoodDiary.Modules.BodyMetrics.Presentation.Contracts.Features.WeightEntries.Responses;

namespace FoodDiary.Modules.BodyMetrics.Presentation.Mappings.Features.WeightEntries.Mappings;

public static class WeightEntrySharedHttpResponseMappings {
    extension(WeightEntryModel model) {
        public WeightEntryHttpResponse ToHttpResponse() {
            return new WeightEntryHttpResponse(model.Id, model.UserId, model.Date, model.WeightKg);
        }
    }

    extension(WeightEntrySummaryModel model) {
        public WeightEntrySummaryHttpResponse ToHttpResponse() {
            return new WeightEntrySummaryHttpResponse(model.StartDate, model.EndDate, model.AverageWeightKg);
        }
    }
}
