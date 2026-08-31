using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Application.Abstractions.Ai.Models;
using FoodDiary.Results;
using FoodDiary.Application.Ai.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Ai.Services;

public sealed class UserAiUsageSummaryReadService(
    IAiUserContextService aiUserContextService,
    IAiUsageReadRepository aiUsageRepository,
    TimeProvider dateTimeProvider)
    : IUserAiUsageSummaryReadService {
    public async Task<Result<UserAiUsageModel>> GetAsync(UserId userId, CancellationToken cancellationToken) {
        Result<AiUserContext> contextResult = await aiUserContextService.GetAsync(userId, cancellationToken).ConfigureAwait(false);
        if (contextResult.IsFailure) {
            return Result.Failure<UserAiUsageModel>(contextResult.Error);
        }

        AiUserContext context = contextResult.Value;
        (DateTime monthStartUtc, DateTime monthEndUtc) = GetMonthBoundsUtc(dateTimeProvider.GetUtcNow().UtcDateTime);
        AiUsageTotals totals = await aiUsageRepository.GetUserTotalsAsync(
            userId,
            monthStartUtc,
            monthEndUtc,
            cancellationToken).ConfigureAwait(false);

        return Result.Success(new UserAiUsageModel(
            context.InputTokenLimit,
            context.OutputTokenLimit,
            totals.InputTokens,
            totals.OutputTokens,
            monthEndUtc));
    }

    private static (DateTime StartUtc, DateTime EndUtc) GetMonthBoundsUtc(DateTime nowUtc) {
        var start = new DateTime(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        return (start, start.AddMonths(1));
    }
}
