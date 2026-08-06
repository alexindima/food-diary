using FoodDiary.Application.WaistEntries.Models;
using FoodDiary.Presentation.Api.Features.WaistEntries.Responses;
using FoodDiary.Presentation.Api.Features.Users.Mappings;

namespace FoodDiary.Presentation.Api.Features.WaistEntries.Mappings;

public static class WaistEntryHttpResponseMappings {
    public static WaistEntryHttpResponse ToHttpResponse(this WaistEntryModel model) {
        return new WaistEntryHttpResponse(model.Id, model.UserId, model.Date, model.Circumference);
    }

    public static WaistEntrySummaryHttpResponse ToHttpResponse(this WaistEntrySummaryModel model) {
        return new WaistEntrySummaryHttpResponse(model.StartDate, model.EndDate, model.AverageCircumference);
    }

    public static WaistHistoryPageSummaryHttpResponse ToHttpResponse(this WaistHistoryPageSummaryModel model) =>
        new(
            [.. model.Entries.Select(static entry => entry.ToHttpResponse())],
            [.. model.Summary.Select(static point => point.ToHttpResponse())],
            model.Height,
            model.Goal.ToHttpResponse(),
            [.. model.GoalHistory.Select(static goal => goal.ToHttpResponse())]);
}
