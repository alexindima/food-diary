using FoodDiary.Application.Abstractions.WeightEntries.Models;
using FoodDiary.Application.BodyMetrics.WeightEntries.Models;
using FoodDiary.Presentation.Api.Features.WeightEntries.Responses;
using FoodDiary.Presentation.Api.Features.Users.Mappings;

namespace FoodDiary.Presentation.Api.Features.WeightEntries.Mappings;

public static class WeightEntryHttpResponseMappings {
    extension(WeightEntryModel model) {
        public WeightEntryHttpResponse ToHttpResponse() {
            return new WeightEntryHttpResponse(model.Id, model.UserId, model.Date, model.Weight);
        }
    }

    extension(WeightEntrySummaryModel model) {
        public WeightEntrySummaryHttpResponse ToHttpResponse() {
            return new WeightEntrySummaryHttpResponse(model.StartDate, model.EndDate, model.AverageWeight);
        }
    }

    extension(WeightHistoryPageSummaryModel model) {
        public WeightHistoryPageSummaryHttpResponse ToHttpResponse() =>
                new(
                    [.. model.Entries.Select(static entry => entry.ToHttpResponse())],
                    [.. model.Summary.Select(static point => point.ToHttpResponse())],
                    model.Height,
                    model.Goal.ToHttpResponse(),
                    [.. model.GoalHistory.Select(static goal => goal.ToHttpResponse())]);
    }
}
