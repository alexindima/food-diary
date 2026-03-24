using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;

namespace FoodDiary.Application.Admin.Queries.GetAdminAiUsageSummary;

public sealed class GetAdminAiUsageSummaryQueryHandler(IAiUsageRepository aiUsageRepository)
    : IQueryHandler<GetAdminAiUsageSummaryQuery, Result<AdminAiUsageSummaryModel>> {
    public async Task<Result<AdminAiUsageSummaryModel>> Handle(
        GetAdminAiUsageSummaryQuery query,
        CancellationToken cancellationToken) {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var from = query.From ?? today.AddDays(-29);
        var to = query.To ?? today.AddDays(1);
        if (from > to) {
            return Result.Failure<AdminAiUsageSummaryModel>(
                Errors.Validation.Invalid("from/to", "'From' date must be less than or equal to 'To' date."));
        }

        var fromUtc = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toUtc = to.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var summary = await aiUsageRepository.GetSummaryAsync(fromUtc, toUtc, cancellationToken);

        var response = new AdminAiUsageSummaryModel(
            summary.TotalTokens,
            summary.InputTokens,
            summary.OutputTokens,
            summary.ByDay.Select(MapDaily).ToList(),
            summary.ByOperation.Select(MapBreakdown).ToList(),
            summary.ByModel.Select(MapBreakdown).ToList(),
            summary.ByUser.Select(MapUser).ToList());

        return Result.Success(response);
    }

    private static AdminAiUsageDailyModel MapDaily(AiUsageDailySummary daily)
        => new(daily.Date, daily.TotalTokens, daily.InputTokens, daily.OutputTokens);

    private static AdminAiUsageBreakdownModel MapBreakdown(AiUsageBreakdown breakdown)
        => new(breakdown.Key, breakdown.TotalTokens, breakdown.InputTokens, breakdown.OutputTokens);

    private static AdminAiUsageUserModel MapUser(AiUsageUserSummary user)
        => new(user.UserId.Value, user.Email, user.TotalTokens, user.InputTokens, user.OutputTokens);
}
