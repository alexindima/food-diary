using FoodDiary.Application.WeightEntries.Models;
using FoodDiary.Presentation.Api.Features.WeightEntries.Responses;
using FoodDiary.Presentation.Api.Features.Users.Mappings;

namespace FoodDiary.Presentation.Api.Features.WeightEntries.Mappings;

public static class WeightEntryHttpResponseMappings {
    public static WeightEntryHttpResponse ToHttpResponse(this WeightEntryModel model) {
        return new WeightEntryHttpResponse(model.Id, model.UserId, model.Date, model.Weight);
    }

    public static WeightEntrySummaryHttpResponse ToHttpResponse(this WeightEntrySummaryModel model) {
        return new WeightEntrySummaryHttpResponse(model.StartDate, model.EndDate, model.AverageWeight);
    }

    public static WeightHistoryPageSummaryHttpResponse ToHttpResponse(this WeightHistoryPageSummaryModel model) =>
        new(
            [.. model.Entries.Select(static entry => entry.ToHttpResponse())],
            [.. model.Summary.Select(static point => point.ToHttpResponse())],
            model.Height,
            model.Goal.ToHttpResponse(),
            [.. model.GoalHistory.Select(static goal => goal.ToHttpResponse())]);
}
