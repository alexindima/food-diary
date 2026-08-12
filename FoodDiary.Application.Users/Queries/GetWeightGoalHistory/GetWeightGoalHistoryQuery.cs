using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.Users.Queries.GetWeightGoalHistory;

public sealed record GetWeightGoalHistoryQuery(Guid? UserId)
    : IQuery<Result<IReadOnlyList<WeightGoalHistoryModel>>>, IUserRequest;
