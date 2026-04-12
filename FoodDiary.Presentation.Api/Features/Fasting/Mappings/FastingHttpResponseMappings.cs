using FoodDiary.Application.Fasting.Models;
using FoodDiary.Application.Common.Models;
using FoodDiary.Presentation.Api.Features.Fasting.Responses;
using FoodDiary.Presentation.Api.Responses;

namespace FoodDiary.Presentation.Api.Features.Fasting.Mappings;

public static class FastingHttpResponseMappings {
    public static FastingSessionHttpResponse ToHttpResponse(this FastingSessionModel model) =>
        new(model.Id, model.StartedAtUtc, model.EndedAtUtc, model.InitialPlannedDurationHours, model.AddedDurationHours, model.PlannedDurationHours,
            model.Protocol, model.PlanType, model.OccurrenceKind, model.CyclicFastDays, model.CyclicEatDays, model.CyclicEatDayFastHours,
            model.CyclicEatDayEatingWindowHours, model.CyclicPhaseDayNumber, model.CyclicPhaseDayTotal, model.IsCompleted, model.Status,
            model.Notes, model.CheckInAtUtc, model.HungerLevel, model.EnergyLevel, model.MoodLevel, model.Symptoms, model.CheckInNotes,
            model.CheckIns.Select(static checkIn => checkIn.ToHttpResponse()).ToList());

    public static FastingCheckInHttpResponse ToHttpResponse(this FastingCheckInModel model) =>
        new(model.Id, model.CheckedInAtUtc, model.HungerLevel, model.EnergyLevel, model.MoodLevel, model.Symptoms, model.Notes);

    public static FastingStatsHttpResponse ToHttpResponse(this FastingStatsModel model) =>
        new(model.TotalCompleted, model.CurrentStreak, model.AverageDurationHours);

    public static FastingInsightsHttpResponse ToHttpResponse(this FastingInsightsModel model) =>
        new(model.Insights.Select(static message => message.ToHttpResponse()).ToList(), model.CurrentPrompt?.ToHttpResponse());

    public static FastingMessageHttpResponse ToHttpResponse(this FastingMessageModel model) =>
        new(model.Id, model.TitleKey, model.BodyKey, model.Tone, model.BodyParams);

    public static PagedHttpResponse<FastingSessionHttpResponse> ToHttpResponse(this PagedResponse<FastingSessionModel> response) =>
        response.ToPagedHttpResponse(ToHttpResponse);
}
