using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Results;
using FoodDiary.Application.Admin.Common;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Ai.Common;

namespace FoodDiary.Application.Admin.Services;

public sealed class AdminAiUsageReadService(
    IAiAdministrationReadService aiReadService,
    TimeProvider dateTimeProvider)
    : IAdminAiUsageReadService {
    public async Task<Result<AdminAiUsageSummaryModel>> GetSummaryAsync(
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken) {
        var today = DateOnly.FromDateTime(dateTimeProvider.GetUtcNow().UtcDateTime);
        DateOnly periodFrom = from ?? today.AddDays(-29);
        DateOnly periodTo = to ?? today.AddDays(1);
        if (periodFrom > periodTo) {
            return Result.Failure<AdminAiUsageSummaryModel>(
                Errors.Validation.Invalid("from/to", "'From' date must be less than or equal to 'To' date."));
        }

        var fromUtc = periodFrom.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toUtc = periodTo.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        AiUsageSummary summary = await aiReadService.GetUsageSummaryAsync(fromUtc, toUtc, cancellationToken).ConfigureAwait(false);

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
