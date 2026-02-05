using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Contracts.Admin;

namespace FoodDiary.Application.Admin.Queries.GetAdminAiUsageSummary;

public sealed class GetAdminAiUsageSummaryQueryHandler(IAiUsageRepository aiUsageRepository)
    : IQueryHandler<GetAdminAiUsageSummaryQuery, Result<AdminAiUsageSummaryResponse>>
{
    public async Task<Result<AdminAiUsageSummaryResponse>> Handle(
        GetAdminAiUsageSummaryQuery query,
        CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var from = query.From ?? today.AddDays(-29);
        var to = query.To ?? today.AddDays(1);

        var fromUtc = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toUtc = to.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var summary = await aiUsageRepository.GetSummaryAsync(fromUtc, toUtc, cancellationToken);

        var response = new AdminAiUsageSummaryResponse(
            summary.TotalTokens,
            summary.InputTokens,
            summary.OutputTokens,
            summary.ByDay.Select(MapDaily).ToList(),
            summary.ByOperation.Select(MapBreakdown).ToList(),
            summary.ByModel.Select(MapBreakdown).ToList());

        return Result.Success(response);
    }

    private static AdminAiUsageDailyResponse MapDaily(AiUsageDailySummary daily)
        => new(daily.Date, daily.TotalTokens, daily.InputTokens, daily.OutputTokens);

    private static AdminAiUsageBreakdownResponse MapBreakdown(AiUsageBreakdown breakdown)
        => new(breakdown.Key, breakdown.TotalTokens, breakdown.InputTokens, breakdown.OutputTokens);
}
