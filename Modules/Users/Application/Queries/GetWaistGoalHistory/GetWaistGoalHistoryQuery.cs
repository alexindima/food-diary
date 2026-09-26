using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Users.Application.Queries.GetWaistGoalHistory;

public sealed record GetWaistGoalHistoryQuery(Guid? UserId)
    : IQuery<Result<IReadOnlyList<WaistGoalHistoryModel>>>, IUserRequest;
