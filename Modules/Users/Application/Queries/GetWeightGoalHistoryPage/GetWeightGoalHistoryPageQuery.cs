using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Users.Application.Queries.GetWeightGoalHistoryPage;

public sealed record GetWeightGoalHistoryPageQuery(Guid? UserId, string? Cursor = null)
    : IQuery<Result<GoalHistoryPageModel<WeightGoalHistoryModel>>>, IUserRequest;
