using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Ai.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Ai.Queries.GetUserAiUsageSummary;

public sealed class GetUserAiUsageSummaryQueryHandler(
    IUserRepository userRepository,
    IAiUsageRepository aiUsageRepository,
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
        User? user = await userRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        Error? accessError = CurrentUserAccessPolicy.EnsureCanAccess(user);
        if (accessError is not null) {
            return Result.Failure<UserAiUsageModel>(accessError);
        }

        User currentUser = user!;
        (DateTime monthStartUtc, DateTime monthEndUtc) = GetMonthBoundsUtc(dateTimeProvider.GetUtcNow().UtcDateTime);
        AiUsageTotals totals = await aiUsageRepository.GetUserTotalsAsync(
            userId,
            monthStartUtc,
            monthEndUtc,
            cancellationToken).ConfigureAwait(false);

        return Result.Success(new UserAiUsageModel(
            currentUser.AiInputTokenLimit,
            currentUser.AiOutputTokenLimit,
            totals.InputTokens,
            totals.OutputTokens,
            monthEndUtc));
    }

    private static (DateTime StartUtc, DateTime EndUtc) GetMonthBoundsUtc(DateTime nowUtc) {
        var start = new DateTime(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        return (start, start.AddMonths(1));
    }
}
