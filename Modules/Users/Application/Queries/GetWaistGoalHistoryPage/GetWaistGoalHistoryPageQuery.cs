using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Users.Application.Queries.GetWaistGoalHistoryPage;

public sealed record GetWaistGoalHistoryPageQuery(Guid? UserId, string? Cursor = null, int Limit = 10)
    : IQuery<Result<GoalHistoryPageModel<WaistGoalHistoryModel>>>, IUserRequest;
