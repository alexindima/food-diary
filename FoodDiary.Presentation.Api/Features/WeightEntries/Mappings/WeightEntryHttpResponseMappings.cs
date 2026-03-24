using FoodDiary.Application.WeightEntries.Models;
using FoodDiary.Presentation.Api.Features.WeightEntries.Responses;

namespace FoodDiary.Presentation.Api.Features.WeightEntries.Mappings;

public static class WeightEntryHttpResponseMappings {
    public static WeightEntryHttpResponse ToHttpResponse(this WeightEntryModel model) {
        return new WeightEntryHttpResponse(model.Id, model.UserId, model.Date, model.Weight);
    }

    public static WeightEntrySummaryHttpResponse ToHttpResponse(this WeightEntrySummaryModel model) {
        return new WeightEntrySummaryHttpResponse(model.StartDate, model.EndDate, model.AverageWeight);
    }
}
