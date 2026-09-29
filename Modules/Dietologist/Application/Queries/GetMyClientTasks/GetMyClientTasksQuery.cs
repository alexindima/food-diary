using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Dietologist.Application.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Dietologist.Application.Queries.GetMyClientTasks;

public sealed record GetMyClientTasksQuery(Guid? UserId, int Page = 1, int Limit = 50)
    : IQuery<Result<IReadOnlyList<ClientTaskModel>>>, IUserRequest;
