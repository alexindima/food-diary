using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Ai.Common;
using FoodDiary.Application.Abstractions.Ai.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Ai.Queries.GetUserAiUsageSummary;

public sealed class GetUserAiUsageSummaryQueryHandler(IUserAiUsageSummaryReadService readService)
    : IQueryHandler<GetUserAiUsageSummaryQuery, Result<UserAiUsageModel>> {
    public async Task<Result<UserAiUsageModel>> Handle(
        GetUserAiUsageSummaryQuery query,
        CancellationToken cancellationToken) {
        if (query.UserId == Guid.Empty) {
            return Result.Failure<UserAiUsageModel>(
                Errors.Validation.Invalid(nameof(query.UserId), "User id must not be empty."));
        }

        var userId = new UserId(query.UserId);
        return await readService.GetAsync(userId, cancellationToken).ConfigureAwait(false);
    }
}
