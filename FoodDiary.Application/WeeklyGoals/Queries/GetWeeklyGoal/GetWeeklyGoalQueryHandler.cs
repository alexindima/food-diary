using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.WeeklyGoals.Common;
using FoodDiary.Application.WeeklyGoals.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.WeeklyGoals.Queries.GetWeeklyGoal;

public sealed class GetWeeklyGoalQueryHandler(
    IWeeklyGoalReadService weeklyGoalReadService,
    IUserContextService userContextService)
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
        WeeklyGoalModel? goal = await weeklyGoalReadService
            .GetAsync(userIdResult.Value, weekStartUtc, cancellationToken)
            .ConfigureAwait(false);
        return Result.Success(goal);
    }
}
