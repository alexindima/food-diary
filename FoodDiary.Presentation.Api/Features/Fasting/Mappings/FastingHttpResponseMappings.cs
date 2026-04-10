using FoodDiary.Application.Fasting.Models;
using FoodDiary.Presentation.Api.Features.Fasting.Responses;

namespace FoodDiary.Presentation.Api.Features.Fasting.Mappings;

public static class FastingHttpResponseMappings {
    public static FastingSessionHttpResponse ToHttpResponse(this FastingSessionModel model) =>
        new(model.Id, model.StartedAtUtc, model.EndedAtUtc, model.InitialPlannedDurationHours, model.AddedDurationHours, model.PlannedDurationHours,
            model.Protocol, model.PlanType, model.OccurrenceKind, model.CyclicFastDays, model.CyclicEatDays, model.CyclicEatDayFastHours,
            model.CyclicEatDayEatingWindowHours, model.IsCompleted, model.Status, model.Notes, model.CheckInAtUtc, model.HungerLevel,
            model.EnergyLevel, model.MoodLevel, model.Symptoms, model.CheckInNotes);

    public static FastingStatsHttpResponse ToHttpResponse(this FastingStatsModel model) =>
        new(model.TotalCompleted, model.CurrentStreak, model.AverageDurationHours);

    public static FastingInsightsHttpResponse ToHttpResponse(this FastingInsightsModel model) =>
        new(model.Insights.Select(static message => message.ToHttpResponse()).ToList(), model.CurrentPrompt?.ToHttpResponse());

    public static FastingMessageHttpResponse ToHttpResponse(this FastingMessageModel model) =>
        new(model.Id, model.TitleKey, model.BodyKey, model.Tone, model.BodyParams);
}
