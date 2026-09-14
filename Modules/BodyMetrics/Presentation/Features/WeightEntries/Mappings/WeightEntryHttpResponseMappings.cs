using FoodDiary.Modules.BodyMetrics.Presentation.Mappings.Features.WeightEntries.Mappings;
using FoodDiary.Modules.BodyMetrics.Application.WeightEntries.Models;
using FoodDiary.Modules.BodyMetrics.Presentation.Features.WeightEntries.Responses;
using FoodDiary.Presentation.Api.Features.Users.Mappings;

namespace FoodDiary.Modules.BodyMetrics.Presentation.Features.WeightEntries.Mappings;

public static class WeightEntryHttpResponseMappings {
    extension(WeightHistoryPageSummaryModel model) {
        public WeightHistoryPageSummaryHttpResponse ToHttpResponse() =>
                new(
                    [.. model.Entries.Select(static entry => entry.ToHttpResponse())],
                    [.. model.Summary.Select(static point => point.ToHttpResponse())],
                    model.HeightCm,
                    model.Goal.ToHttpResponse(),
                    [.. model.GoalHistory.Select(static goal => goal.ToHttpResponse())]);
    }
}
