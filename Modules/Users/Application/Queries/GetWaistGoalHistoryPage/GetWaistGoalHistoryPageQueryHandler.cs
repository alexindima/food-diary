using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Application.Contracts.Common.Abstractions.Results;
using FoodDiary.Application.Contracts.Common.Validation;
using FoodDiary.Modules.Users.Application.Common;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Users.Application.Queries.GetWaistGoalHistoryPage;

public sealed class GetWaistGoalHistoryPageQueryHandler(IUserBodyMetricHistoryReadService reader, ICurrentUserAccessService access,
    TimeProvider timeProvider) : IQueryHandler<GetWaistGoalHistoryPageQuery, Result<GoalHistoryPageModel<WaistGoalHistoryModel>>> {
    public async Task<Result<GoalHistoryPageModel<WaistGoalHistoryModel>>> Handle(GetWaistGoalHistoryPageQuery query, CancellationToken cancellationToken) {
        Result<UserId> userId = await CurrentUserAccessResolver.ResolveAsync(query.UserId, access, cancellationToken).ConfigureAwait(false);
        if (userId.IsFailure) { return Result.Failure<GoalHistoryPageModel<WaistGoalHistoryModel>>(userId.Error); }
        if (!GoalHistoryCursor.TryDecode(query.Cursor, timeProvider.GetUtcNow().UtcDateTime, out DateTime snapshotUtc, out int offset)) {
            return Result.Failure<GoalHistoryPageModel<WaistGoalHistoryModel>>(Errors.Validation.Invalid(nameof(query.Cursor), "Invalid history cursor."));
        }
        int limit = PaginationPolicy.NormalizePageSize(query.Limit);
        Result<IReadOnlyList<WaistGoalHistoryModel>> result = await reader.ReadWaistGoalsAsync(userId.Value, snapshotUtc,
            offset, limit + 1, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure) { return Result.Failure<GoalHistoryPageModel<WaistGoalHistoryModel>>(result.Error); }
        bool hasMore = result.Value.Count > limit;
        return Result.Success(new GoalHistoryPageModel<WaistGoalHistoryModel>(result.Value.Take(limit).ToArray(),
            hasMore ? GoalHistoryCursor.Encode(snapshotUtc, offset + limit) : null));
    }
}
