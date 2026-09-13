using FoodDiary.Modules.Fasting.Contracts.Read.Models;
using FoodDiary.Presentation.Api.Features.Fasting.Responses;

namespace FoodDiary.Presentation.Api.Features.Fasting.Mappings;

public static class FastingSessionHttpResponseMappings {
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
}
