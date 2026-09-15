using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.WeeklyGoals.Contracts.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.WeeklyGoals.Application.Commands.UpsertWeeklyGoal;

public sealed record UpsertWeeklyGoalCommand(
    Guid? UserId,
    DateOnly WeekStart,
    int TargetDays,
    bool ReminderEnabled,
    TimeOnly? ReminderTime,
    int? TimeZoneOffsetMinutes) : ICommand<Result<WeeklyGoalModel>>, IUserRequest;
