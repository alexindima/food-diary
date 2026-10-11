using FoodDiary.Application.Contracts.Common.Validation;
using FoodDiary.Modules.WeeklyGoals.Application.Abstractions.Common;
using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Application.Contracts.Common.Abstractions.Results;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.WeeklyGoals.Application.Common;
using FoodDiary.Modules.WeeklyGoals.Contracts.Models;
using FoodDiary.Modules.WeeklyGoals.Domain.Entities;
using FoodDiary.Modules.WeeklyGoals.Domain.Enums;
using FoodDiary.Modules.WeeklyGoals.Domain.ValueObjects;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.WeeklyGoals.Application.Commands.UpsertWeeklyGoal;

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

        if (!LocalCalendar.TryResolve(command.TimeZoneId, offsetMinutes: null, out TimeZoneInfo timeZone)) {
            return Result.Failure<WeeklyGoalModel>(Errors.Validation.Invalid(nameof(command.TimeZoneId), "Provide a valid calendar time zone."));
        }
        UserId userId = userIdResult.Value;
        var weekStartUtc = DateTime.SpecifyKind(command.WeekStart.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        DateTime utcNow = timeProvider.GetUtcNow().UtcDateTime;
        if (!WeeklyGoalWeekPolicy.CanWrite(command.WeekStart, command.TimeZoneId is null ? utcNow : LocalCalendar.DateAt(utcNow, timeZone).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc))) {
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
                var reminder = WeeklyGoalReminderSettings.FromMinutes(
                    command.ReminderEnabled, reminderMinutes, command.TimeZoneOffsetMinutes);
                if (goal is null) {
                    goal = WeeklyGoal.CreateWithReminder(
                        userId,
                        weekStartUtc,
                        WeeklyGoalType.DiaryLogging,
                        command.TargetDays,
                        reminder);
                    await goalRepository.AddAsync(goal, token).ConfigureAwait(false);
                } else {
                    goal.UpdateWithReminder(
                        command.TargetDays,
                        reminder,
                        utcNow);
                }

                int progressDays = await progressReader.GetProgressDaysAsync(goal, token, command.TimeZoneId is null ? null : timeZone).ConfigureAwait(false);
                return Result.Success(goal.ToModel(progressDays));
            },
            cancellationToken).ConfigureAwait(false);
    }
}
