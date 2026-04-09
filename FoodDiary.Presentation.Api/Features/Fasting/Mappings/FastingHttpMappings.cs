using FoodDiary.Application.Fasting.Commands.EndFasting;
using FoodDiary.Application.Fasting.Commands.ExtendActiveFasting;
using FoodDiary.Application.Fasting.Commands.PostponeCyclicFastDay;
using FoodDiary.Application.Fasting.Commands.SkipCyclicFastDay;
using FoodDiary.Application.Fasting.Commands.StartFasting;
using FoodDiary.Application.Fasting.Queries.GetCurrentFasting;
using FoodDiary.Application.Fasting.Queries.GetFastingHistory;
using FoodDiary.Application.Fasting.Queries.GetFastingStats;
using FoodDiary.Presentation.Api.Features.Fasting.Requests;

namespace FoodDiary.Presentation.Api.Features.Fasting.Mappings;

public static class FastingHttpMappings {
    public static StartFastingCommand ToCommand(this StartFastingHttpRequest request, Guid userId) =>
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

    public static EndFastingCommand ToEndCommand(this Guid userId) => new(userId);

    public static SkipCyclicFastDayCommand ToSkipCyclicFastDayCommand(this Guid userId) => new(userId);

    public static PostponeCyclicFastDayCommand ToPostponeCyclicFastDayCommand(this Guid userId) => new(userId);

    public static ExtendActiveFastingCommand ToExtendCommand(this ExtendActiveFastingHttpRequest request, Guid userId) =>
        new(userId, request.AdditionalHours);

    public static GetCurrentFastingQuery ToCurrentQuery(this Guid userId) => new(userId);

    public static GetFastingHistoryQuery ToHistoryQuery(this GetFastingHistoryHttpQuery query, Guid userId) =>
        new(userId, query.From, query.To);

    public static GetFastingStatsQuery ToStatsQuery(this Guid userId) => new(userId);
}
