using FoodDiary.Application.BodyMetrics.WaistEntries.Models;
using FoodDiary.Presentation.Api.Features.WaistEntries.Responses;
using FoodDiary.Presentation.Api.Features.Users.Mappings;

namespace FoodDiary.Presentation.Api.Features.WaistEntries.Mappings;

public static class WaistEntryHttpResponseMappings {
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
