using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.WeeklyGoals.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.WeeklyGoals.Queries.GetWeeklyGoal;

public sealed record GetWeeklyGoalQuery(Guid? UserId, DateOnly WeekStart)
    : IQuery<Result<WeeklyGoalModel?>>, IUserRequest;
