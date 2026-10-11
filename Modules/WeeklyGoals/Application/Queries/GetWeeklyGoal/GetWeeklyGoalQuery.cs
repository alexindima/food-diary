using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.WeeklyGoals.Contracts.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.WeeklyGoals.Application.Queries.GetWeeklyGoal;

public sealed record GetWeeklyGoalQuery(Guid? UserId, DateOnly WeekStart, string? TimeZoneId = null)
    : IQuery<Result<WeeklyGoalModel?>>, IUserRequest;
