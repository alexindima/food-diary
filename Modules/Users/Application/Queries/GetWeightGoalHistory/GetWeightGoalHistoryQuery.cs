using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Users.Application.Queries.GetWeightGoalHistory;

public sealed record GetWeightGoalHistoryQuery(Guid? UserId)
    : IQuery<Result<IReadOnlyList<WeightGoalHistoryModel>>>, IUserRequest;
