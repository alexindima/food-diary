using FoodDiary.Modules.Fasting.Contracts.Read.Models;
using FoodDiary.Application.Abstractions.Common.Models;
using FoodDiary.Presentation.Api.Features.Fasting.Responses;
using FoodDiary.Presentation.Api.Responses;

namespace FoodDiary.Presentation.Api.Features.Fasting.Mappings;

public static class FastingHttpResponseMappings {
    extension(FastingSessionModel model) {
        public FastingSessionHttpResponse ToHttpResponse() =>
                new(model.Id, model.StartedAtUtc, model.EndedAtUtc, model.InitialPlannedDurationHours, model.AddedDurationHours, model.PlannedDurationHours,
                    model.Protocol, model.PlanType, model.OccurrenceKind, model.CyclicFastDays, model.CyclicEatDays, model.CyclicEatDayFastHours,
                    model.CyclicEatDayEatingWindowHours, model.CyclicPhaseDayNumber, model.CyclicPhaseDayTotal, model.IsCompleted, model.Status,
                    model.Notes, model.CheckInAtUtc, model.HungerLevel, model.EnergyLevel, model.MoodLevel, model.Symptoms, model.CheckInNotes,
                    model.CheckIns.Select(static checkIn => checkIn.ToHttpResponse()).ToList());
    }

    extension(FastingCheckInModel model) {
        public FastingCheckInHttpResponse ToHttpResponse() =>
                new(model.Id, model.CheckedInAtUtc, model.HungerLevel, model.EnergyLevel, model.MoodLevel, model.Symptoms, model.Notes);
    }

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
                response.ToPagedHttpResponse(ToHttpResponse);
    }
}
