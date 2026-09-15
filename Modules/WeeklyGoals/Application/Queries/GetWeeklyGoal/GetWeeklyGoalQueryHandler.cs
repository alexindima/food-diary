using FoodDiary.Modules.WeeklyGoals.Application.Abstractions.Common;
using FoodDiary.Modules.WeeklyGoals.Application.Common;
using FoodDiary.Modules.WeeklyGoals.Contracts.Models;
using FoodDiary.Modules.WeeklyGoals.Application.Abstractions.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Results;

namespace FoodDiary.Modules.WeeklyGoals.Application.Queries.GetWeeklyGoal;

public sealed class GetWeeklyGoalQueryHandler(
    IWeeklyGoalRepository goalRepository,
    WeeklyGoalProgressReader progressReader,
    ICurrentUserAccessService userContextService)
    : IQueryHandler<GetWeeklyGoalQuery, Result<WeeklyGoalModel?>> {
    public async Task<Result<WeeklyGoalModel?>> Handle(GetWeeklyGoalQuery query, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            userContextService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<WeeklyGoalModel?>(userIdResult);
        }

        var weekStartUtc = DateTime.SpecifyKind(query.WeekStart.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        WeeklyGoalModel? goal = await GetGoalAsync(userIdResult.Value, weekStartUtc, cancellationToken)
            .ConfigureAwait(false);
        return Result.Success(goal);
    }
    private async Task<WeeklyGoalModel?> GetGoalAsync(
        UserId userId,
        DateTime weekStartUtc,
        CancellationToken cancellationToken) {
        WeeklyGoalReadModel? goal = await goalRepository
            .GetReadModelAsync(userId, weekStartUtc, cancellationToken)
            .ConfigureAwait(false);
        if (goal is null) {
            return null;
        }

        int progressDays = await progressReader.GetProgressDaysAsync(userId, goal.WeekStartUtc, cancellationToken).ConfigureAwait(false);
        return goal.ToModel(progressDays);
    }
}
