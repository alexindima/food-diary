using FoodDiary.Application.Fasting.Models;
using FoodDiary.Presentation.Api.Features.Fasting.Responses;

namespace FoodDiary.Presentation.Api.Features.Fasting.Mappings;

public static class FastingHttpResponseMappings {
    public static FastingSessionHttpResponse ToHttpResponse(this FastingSessionModel model) =>
        new(model.Id, model.StartedAtUtc, model.EndedAtUtc, model.PlannedDurationHours,
            model.Protocol, model.IsCompleted, model.Notes);

    public static FastingStatsHttpResponse ToHttpResponse(this FastingStatsModel model) =>
        new(model.TotalCompleted, model.CurrentStreak, model.AverageDurationHours);
}
