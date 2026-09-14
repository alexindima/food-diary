using FoodDiary.Modules.BodyMetrics.Presentation.Mappings.Features.WaistEntries.Mappings;
using FoodDiary.Modules.BodyMetrics.Application.WaistEntries.Models;
using FoodDiary.Modules.BodyMetrics.Presentation.Features.WaistEntries.Responses;
using FoodDiary.Presentation.Api.Features.Users.Mappings;

namespace FoodDiary.Modules.BodyMetrics.Presentation.Features.WaistEntries.Mappings;

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
