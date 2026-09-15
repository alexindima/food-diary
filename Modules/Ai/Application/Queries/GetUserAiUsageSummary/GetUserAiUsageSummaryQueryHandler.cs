using FoodDiary.Modules.Users.Contracts.Common.Validation;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

using FoodDiary.Modules.Ai.Application.Abstractions.Models;

namespace FoodDiary.Modules.Ai.Application.Queries.GetUserAiUsageSummary;

public sealed class GetUserAiUsageSummaryQueryHandler(
    IUserAiProfileReadService userProfileReadService,
    IAiUsageQuery aiUsageRepository,
    TimeProvider dateTimeProvider)
    : IQueryHandler<GetUserAiUsageSummaryQuery, Result<UserAiUsageModel>> {
    public async Task<Result<UserAiUsageModel>> Handle(
        GetUserAiUsageSummaryQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(
            query.UserId,
            Errors.Validation.Invalid(nameof(query.UserId), "User id must not be empty."));
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure<UserAiUsageModel>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        Result<UserAiProfileModel> contextResult = await userProfileReadService.GetAiProfileAsync(userId, cancellationToken).ConfigureAwait(false);
        if (contextResult.IsFailure) {
            return Result.Failure<UserAiUsageModel>(contextResult.Error);
        }

        UserAiProfileModel context = contextResult.Value;
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
