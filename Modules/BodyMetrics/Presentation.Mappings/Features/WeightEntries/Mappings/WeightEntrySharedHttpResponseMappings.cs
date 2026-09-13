using FoodDiary.Application.Abstractions.WeightEntries.Models;
using FoodDiary.Presentation.Api.Features.WeightEntries.Responses;

namespace FoodDiary.Presentation.Api.Features.WeightEntries.Mappings;

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
