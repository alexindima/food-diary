using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Application.Ai.Common;
using FoodDiary.Application.Abstractions.Ai.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Ai.Queries.GetUserAiUsageSummary;

public sealed class GetUserAiUsageSummaryQueryHandler(
    IAiUserContextService aiUserContextService,
    IAiUsageReadRepository aiUsageRepository,
    TimeProvider dateTimeProvider)
    : IQueryHandler<GetUserAiUsageSummaryQuery, Result<UserAiUsageModel>> {
    public async Task<Result<UserAiUsageModel>> Handle(
        GetUserAiUsageSummaryQuery query,
        CancellationToken cancellationToken) {
        if (query.UserId == Guid.Empty) {
            return Result.Failure<UserAiUsageModel>(
                Errors.Validation.Invalid(nameof(query.UserId), "User id must not be empty."));
        }

        var userId = new UserId(query.UserId);
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
