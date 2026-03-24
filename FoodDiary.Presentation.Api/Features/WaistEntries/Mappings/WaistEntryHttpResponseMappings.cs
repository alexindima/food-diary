using FoodDiary.Application.WaistEntries.Models;
using FoodDiary.Presentation.Api.Features.WaistEntries.Responses;

namespace FoodDiary.Presentation.Api.Features.WaistEntries.Mappings;

public static class WaistEntryHttpResponseMappings {
    public static WaistEntryHttpResponse ToHttpResponse(this WaistEntryModel model) {
        return new WaistEntryHttpResponse(model.Id, model.UserId, model.Date, model.Circumference);
    }

    public static WaistEntrySummaryHttpResponse ToHttpResponse(this WaistEntrySummaryModel model) {
        return new WaistEntrySummaryHttpResponse(model.StartDate, model.EndDate, model.AverageCircumference);
    }
}
