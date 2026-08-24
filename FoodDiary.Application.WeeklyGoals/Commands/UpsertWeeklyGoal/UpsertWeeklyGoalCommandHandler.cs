using FoodDiary.Application.Abstractions.WeeklyGoals.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
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
    IWeeklyGoalTransactionRunner transactionRunner,
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
        DateTime utcNow = timeProvider.GetUtcNow().UtcDateTime;
        if (!WeeklyGoalWeekPolicy.CanWrite(command.WeekStart, utcNow)) {
            return Result.Failure<WeeklyGoalModel>(Errors.Validation.Invalid(
                nameof(command.WeekStart),
                "A weekly goal can be changed only for an adjacent current week."));
        }

        int? reminderMinutes = command.ReminderTime is { } reminderTime
            ? (int)reminderTime.ToTimeSpan().TotalMinutes
            : null;
        return await transactionRunner.ExecuteSerializedAsync(
            userId,
            weekStartUtc,
            async token => {
                WeeklyGoal? goal = await goalRepository.GetAsync(userId, weekStartUtc, asTracking: true, token).ConfigureAwait(false);
                if (goal is null) {
                    goal = WeeklyGoal.Create(
                        userId,
                        weekStartUtc,
                        WeeklyGoalType.DiaryLogging,
                        command.TargetDays,
                        command.ReminderEnabled,
                        reminderMinutes,
                        command.TimeZoneOffsetMinutes);
                    await goalRepository.AddAsync(goal, token).ConfigureAwait(false);
                } else {
                    goal.Update(
                        command.TargetDays,
                        command.ReminderEnabled,
                        reminderMinutes,
                        command.TimeZoneOffsetMinutes,
                        utcNow);
                }

                int progressDays = await progressReader.GetProgressDaysAsync(goal, token).ConfigureAwait(false);
                return Result.Success(goal.ToModel(progressDays));
            },
            cancellationToken).ConfigureAwait(false);
    }
}
