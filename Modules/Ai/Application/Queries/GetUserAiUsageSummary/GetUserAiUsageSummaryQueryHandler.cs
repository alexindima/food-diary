using FoodDiary.Modules.Users.Contracts.Common.Validation;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Application.Contracts.Common.Abstractions.Results;
using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Results;

using FoodDiary.Modules.Ai.Application.Abstractions.Models;
using FoodDiary.Modules.Ai.Application.Services;

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
        Result<UserAiProfileModel> contextResult = await userProfileReadService.GetAiProfileAsync(userId, cancellationToken);
        if (contextResult.IsFailure) {
            return Result.Failure<UserAiUsageModel>(contextResult.Error);
        }

        UserAiProfileModel context = contextResult.Value;
        var window = AiQuotaWindow.Resolve(context, dateTimeProvider.GetUtcNow().UtcDateTime);
        AiUsageTotals totals = await aiUsageRepository.GetUserTotalsAsync(
            userId,
            window.StartUtc,
            window.EndUtc,
            window.IsTrial,
            cancellationToken);

        return Result.Success(new UserAiUsageModel(
            window.InputLimit,
            window.OutputLimit,
            totals.InputTokens,
            totals.OutputTokens,
            window.EndUtc));
    }
}
