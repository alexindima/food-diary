using FoodDiary.Application.Abstractions.WeeklyGoals.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.WeeklyGoals.Common;
using FoodDiary.Application.WeeklyGoals.Models;
using FoodDiary.Domain.Entities.WeeklyGoals;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.WeeklyGoals.Commands.UpsertWeeklyGoal;

public sealed class UpsertWeeklyGoalCommandHandler(
    IWeeklyGoalRepository goalRepository,
    WeeklyGoalProgressReader progressReader,
    ICurrentUserAccessService userContextService,
    TimeProvider timeProvider)
    : ICommandHandler<UpsertWeeklyGoalCommand, Result<WeeklyGoalModel>> {
    public async Task<Result<WeeklyGoalModel>> Handle(UpsertWeeklyGoalCommand command, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId,
            userContextService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<WeeklyGoalModel>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        var weekStartUtc = DateTime.SpecifyKind(command.WeekStart.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        int? reminderMinutes = command.ReminderTime is { } reminderTime
            ? (int)reminderTime.ToTimeSpan().TotalMinutes
            : null;
        WeeklyGoal? goal = await goalRepository.GetAsync(userId, weekStartUtc, asTracking: true, cancellationToken).ConfigureAwait(false);
        if (goal is null) {
            goal = WeeklyGoal.Create(
                userId,
                weekStartUtc,
                WeeklyGoalType.DiaryLogging,
                command.TargetDays,
                command.ReminderEnabled,
                reminderMinutes,
                command.TimeZoneOffsetMinutes);
            await goalRepository.AddAsync(goal, cancellationToken).ConfigureAwait(false);
        } else {
            goal.Update(
                command.TargetDays,
                command.ReminderEnabled,
                reminderMinutes,
                command.TimeZoneOffsetMinutes,
                timeProvider.GetUtcNow().UtcDateTime);
        }

        int progressDays = await progressReader.GetProgressDaysAsync(goal, cancellationToken).ConfigureAwait(false);
        return Result.Success(goal.ToModel(progressDays));
    }
}
