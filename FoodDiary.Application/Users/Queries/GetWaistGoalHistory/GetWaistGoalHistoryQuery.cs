using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Users.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.Users.Queries.GetWaistGoalHistory;

public sealed record GetWaistGoalHistoryQuery(Guid? UserId)
    : IQuery<Result<IReadOnlyList<WaistGoalHistoryModel>>>, IUserRequest;
