using FoodDiary.Application.Abstractions.WaistEntries.Models;
using FoodDiary.Presentation.Api.Features.WaistEntries.Responses;

namespace FoodDiary.Presentation.Api.Features.WaistEntries.Mappings;

public static class WaistEntrySharedHttpResponseMappings {
    extension(WaistEntryModel model) {
        public WaistEntryHttpResponse ToHttpResponse() {
            return new WaistEntryHttpResponse(model.Id, model.UserId, model.Date, model.CircumferenceCm);
        }
    }

    extension(WaistEntrySummaryModel model) {
        public WaistEntrySummaryHttpResponse ToHttpResponse() {
            return new WaistEntrySummaryHttpResponse(model.StartDate, model.EndDate, model.AverageCircumferenceCm);
        }
    }
}
