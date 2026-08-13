using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.WeeklyGoals.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.WeeklyGoals.Commands.UpsertWeeklyGoal;

public sealed record UpsertWeeklyGoalCommand(
    Guid? UserId,
    DateOnly WeekStart,
    int TargetDays,
    bool ReminderEnabled,
    TimeOnly? ReminderTime,
    int? TimeZoneOffsetMinutes) : ICommand<Result<WeeklyGoalModel>>, IUserRequest;
