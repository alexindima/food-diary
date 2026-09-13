using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Admin.Application.Models;

namespace FoodDiary.Modules.Admin.Application.Queries.GetAdminAiUsageSummary;

public sealed class GetAdminAiUsageSummaryQueryHandler(
    IAiAdministrationReadService aiReadService,
    TimeProvider dateTimeProvider)
    : IQueryHandler<GetAdminAiUsageSummaryQuery, Result<AdminAiUsageSummaryModel>> {
    public async Task<Result<AdminAiUsageSummaryModel>> Handle(GetAdminAiUsageSummaryQuery query, CancellationToken cancellationToken) {
        var today = DateOnly.FromDateTime(dateTimeProvider.GetUtcNow().UtcDateTime);
        DateOnly periodFrom = query.From ?? today.AddDays(-29);
        DateOnly periodTo = query.To ?? today.AddDays(1);
        if (periodFrom > periodTo) {
            return Result.Failure<AdminAiUsageSummaryModel>(
                Errors.Validation.Invalid("from/to", "'From' date must be less than or equal to 'To' date."));
        }

        var fromUtc = periodFrom.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toUtc = periodTo.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        AiUsageSummary summary = query.UserId.HasValue
            ? await aiReadService.GetUsageSummaryForUserAsync(fromUtc, toUtc, query.UserId.Value, cancellationToken).ConfigureAwait(false)
            : await aiReadService.GetUsageSummaryAsync(fromUtc, toUtc, cancellationToken).ConfigureAwait(false);

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
