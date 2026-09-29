using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Application.Contracts.Common.Abstractions.Results;
using FoodDiary.Application.Contracts.Common.Validation;
using FoodDiary.Modules.Users.Application.Common;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Users.Application.Queries.GetWeightGoalHistoryPage;

public sealed class GetWeightGoalHistoryPageQueryHandler(IUserBodyMetricHistoryReadService reader, ICurrentUserAccessService access,
    TimeProvider timeProvider) : IQueryHandler<GetWeightGoalHistoryPageQuery, Result<GoalHistoryPageModel<WeightGoalHistoryModel>>> {
    public async Task<Result<GoalHistoryPageModel<WeightGoalHistoryModel>>> Handle(GetWeightGoalHistoryPageQuery query, CancellationToken cancellationToken) {
        Result<UserId> userId = await CurrentUserAccessResolver.ResolveAsync(query.UserId, access, cancellationToken).ConfigureAwait(false);
        if (userId.IsFailure) { return Result.Failure<GoalHistoryPageModel<WeightGoalHistoryModel>>(userId.Error); }
        if (!GoalHistoryCursor.TryDecode(query.Cursor, timeProvider.GetUtcNow().UtcDateTime, out DateTime snapshotUtc, out int offset)) {
            return Result.Failure<GoalHistoryPageModel<WeightGoalHistoryModel>>(Errors.Validation.Invalid(nameof(query.Cursor), "Invalid history cursor."));
        }
        int limit = PaginationPolicy.NormalizePageSize(query.Limit);
        Result<IReadOnlyList<WeightGoalHistoryModel>> result = await reader.ReadWeightGoalsAsync(userId.Value, snapshotUtc,
            offset, limit + 1, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure) { return Result.Failure<GoalHistoryPageModel<WeightGoalHistoryModel>>(result.Error); }
        bool hasMore = result.Value.Count > limit;
        return Result.Success(new GoalHistoryPageModel<WeightGoalHistoryModel>(result.Value.Take(limit).ToArray(),
            hasMore ? GoalHistoryCursor.Encode(snapshotUtc, offset + limit) : null));
    }
}
