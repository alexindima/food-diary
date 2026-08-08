using FoodDiary.Application.Fasting.Commands.EndFasting;
using FoodDiary.Application.Fasting.Commands.ExtendActiveFasting;
using FoodDiary.Application.Fasting.Commands.PostponeCyclicDay;
using FoodDiary.Application.Fasting.Commands.ReduceActiveFastingTarget;
using FoodDiary.Application.Fasting.Commands.SkipCyclicDay;
using FoodDiary.Application.Fasting.Commands.StartFasting;
using FoodDiary.Application.Fasting.Commands.UpdateCurrentFastingCheckIn;
using FoodDiary.Application.Fasting.Queries.GetCurrentFasting;
using FoodDiary.Application.Fasting.Queries.GetFastingHistory;
using FoodDiary.Application.Fasting.Queries.GetFastingInsights;
using FoodDiary.Application.Fasting.Queries.GetFastingOverview;
using FoodDiary.Application.Fasting.Queries.GetFastingStats;
using FoodDiary.Presentation.Api.Features.Fasting.Requests;

namespace FoodDiary.Presentation.Api.Features.Fasting.Mappings;

public static class FastingHttpMappings {
    extension(StartFastingHttpRequest request) {
        public StartFastingCommand ToCommand(Guid userId) =>
                new(
                    userId,
                    request.Protocol,
                    request.PlanType,
                    request.PlannedDurationHours,
                    request.CyclicFastDays,
                    request.CyclicEatDays,
                    request.CyclicEatDayFastHours,
                    request.CyclicEatDayEatingWindowHours,
                    request.Notes);
    }

    extension(Guid userId) {
        public EndFastingCommand ToEndCommand() => new(userId);
        public SkipCyclicDayCommand ToSkipCyclicDayCommand() => new(userId);
        public PostponeCyclicDayCommand ToPostponeCyclicDayCommand() => new(userId);
    }

    extension(ExtendActiveFastingHttpRequest request) {
        public ExtendActiveFastingCommand ToExtendCommand(Guid userId) =>
                new(userId, request.AdditionalHours);
    }

    extension(ReduceActiveFastingTargetHttpRequest request) {
        public ReduceActiveFastingTargetCommand ToReduceCommand(Guid userId) =>
                new(userId, request.ReducedHours);
    }

    extension(UpdateFastingCheckInHttpRequest request) {
        public UpdateCurrentFastingCheckInCommand ToCheckInCommand(Guid userId) =>
                new(userId, request.HungerLevel, request.EnergyLevel, request.MoodLevel, request.Symptoms, request.CheckInNotes);
    }

    extension(Guid userId) {
        public GetCurrentFastingQuery ToCurrentQuery() => new(userId);
        public GetFastingOverviewQuery ToOverviewQuery() => new(userId);
    }

    extension(GetFastingHistoryHttpQuery query) {
        public GetFastingHistoryQuery ToHistoryQuery(Guid userId) =>
                new(userId, query.From, query.To, query.Page, query.Limit);
    }

    extension(Guid userId) {
        public GetFastingStatsQuery ToStatsQuery() => new(userId);
        public GetFastingInsightsQuery ToInsightsQuery() => new(userId);
    }
}
