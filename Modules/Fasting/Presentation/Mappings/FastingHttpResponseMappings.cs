using FoodDiary.Modules.Fasting.Presentation.Mappings.Mappings;
using FoodDiary.Modules.Fasting.Contracts.Read.Models;
using FoodDiary.Application.Abstractions.Common.Models;
using FoodDiary.Modules.Fasting.Presentation.Contracts.Responses;
using FoodDiary.Modules.Fasting.Presentation.Responses;
using FoodDiary.Presentation.Api.Responses;

namespace FoodDiary.Modules.Fasting.Presentation.Mappings;

public static class FastingHttpResponseMappings {
    extension(FastingStatsModel model) {
        public FastingStatsHttpResponse ToHttpResponse() =>
                new(
                    model.TotalCompleted,
                    model.CurrentStreak,
                    model.AverageDurationHours,
                    model.CompletionRateLast30Days,
                    model.CheckInRateLast30Days,
                    model.LastCheckInAtUtc,
                    model.TopSymptom);
    }

    extension(FastingInsightsModel model) {
        public FastingInsightsHttpResponse ToHttpResponse() =>
                new(
                    model.Alerts.Select(static message => message.ToHttpResponse()).ToList(),
                    model.Insights.Select(static message => message.ToHttpResponse()).ToList());
    }

    extension(FastingOverviewModel model) {
        public FastingOverviewHttpResponse ToHttpResponse() =>
                new(
                    model.CurrentSession?.ToHttpResponse(),
                    model.Stats.ToHttpResponse(),
                    model.Insights.ToHttpResponse(),
                    model.History.ToHttpResponse());
    }

    extension(FastingMessageModel model) {
        private FastingMessageHttpResponse ToHttpResponse() =>
                new(model.Id, model.TitleKey, model.BodyKey, model.Tone, model.BodyParams);
    }

    extension(PagedResponse<FastingSessionModel> response) {
        public PagedHttpResponse<FastingSessionHttpResponse> ToHttpResponse() =>
                response.ToPagedHttpResponse(FastingSessionHttpResponseMappings.ToHttpResponse);
    }
}
