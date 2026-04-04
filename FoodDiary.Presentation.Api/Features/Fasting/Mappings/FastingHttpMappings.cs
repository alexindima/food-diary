using FoodDiary.Application.Fasting.Commands.EndFasting;
using FoodDiary.Application.Fasting.Commands.StartFasting;
using FoodDiary.Application.Fasting.Queries.GetCurrentFasting;
using FoodDiary.Application.Fasting.Queries.GetFastingHistory;
using FoodDiary.Application.Fasting.Queries.GetFastingStats;
using FoodDiary.Presentation.Api.Features.Fasting.Requests;

namespace FoodDiary.Presentation.Api.Features.Fasting.Mappings;

public static class FastingHttpMappings {
    public static StartFastingCommand ToCommand(this StartFastingHttpRequest request, Guid userId) =>
        new(userId, request.Protocol, request.PlannedDurationHours, request.Notes);

    public static EndFastingCommand ToEndCommand(this Guid userId) => new(userId);

    public static GetCurrentFastingQuery ToCurrentQuery(this Guid userId) => new(userId);

    public static GetFastingHistoryQuery ToHistoryQuery(this GetFastingHistoryHttpQuery query, Guid userId) =>
        new(userId, query.From, query.To);

    public static GetFastingStatsQuery ToStatsQuery(this Guid userId) => new(userId);
}
