using FoodDiary.Application.Abstractions.WaistEntries.Models;
using FoodDiary.Application.BodyMetrics.WaistEntries.Models;
using FoodDiary.Presentation.Api.Features.WaistEntries.Responses;
using FoodDiary.Presentation.Api.Features.Users.Mappings;

namespace FoodDiary.Presentation.Api.Features.WaistEntries.Mappings;

public static class WaistEntryHttpResponseMappings {
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

    extension(WaistHistoryPageSummaryModel model) {
        public WaistHistoryPageSummaryHttpResponse ToHttpResponse() =>
                new(
                    [.. model.Entries.Select(static entry => entry.ToHttpResponse())],
                    [.. model.Summary.Select(static point => point.ToHttpResponse())],
                    model.HeightCm,
                    model.Goal.ToHttpResponse(),
                    [.. model.GoalHistory.Select(static goal => goal.ToHttpResponse())]);
    }
}
