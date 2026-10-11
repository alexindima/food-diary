using FoodDiary.Application.Contracts.Common.Validation;
using FoodDiary.Application.Contracts.Common.Abstractions.Results;
using FoodDiary.Modules.WeeklyGoals.Application.Abstractions.Common;
using FoodDiary.Modules.WeeklyGoals.Application.Common;
using FoodDiary.Modules.WeeklyGoals.Contracts.Models;
using FoodDiary.Modules.WeeklyGoals.Application.Abstractions.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
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

        if (!LocalCalendar.TryResolve(query.TimeZoneId, offsetMinutes: null, out TimeZoneInfo timeZone)) {
            return Result.Failure<WeeklyGoalModel?>(Errors.Validation.Invalid(nameof(query.TimeZoneId), "Provide a valid calendar time zone."));
        }
        var weekStartUtc = DateTime.SpecifyKind(query.WeekStart.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        WeeklyGoalModel? goal = await GetGoalAsync(userIdResult.Value, weekStartUtc, query.TimeZoneId is null ? null : timeZone, cancellationToken)
            .ConfigureAwait(false);
        return Result.Success(goal);
    }
    private async Task<WeeklyGoalModel?> GetGoalAsync(
        UserId userId,
        DateTime weekStartUtc,
        TimeZoneInfo? timeZone,
        CancellationToken cancellationToken) {
        WeeklyGoalReadModel? goal = await goalRepository
            .GetReadModelAsync(userId, weekStartUtc, cancellationToken)
            .ConfigureAwait(false);
        if (goal is null) {
            return null;
        }

        int progressDays = await progressReader.GetProgressDaysAsync(userId, goal.WeekStartUtc, cancellationToken, timeZone).ConfigureAwait(false);
        return goal.ToModel(progressDays);
    }
}
