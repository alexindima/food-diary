using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Dietologist.Application.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Dietologist.Application.Queries.GetMyClientTasks;

public sealed record GetMyClientTasksQuery(Guid? UserId)
    : IQuery<Result<IReadOnlyList<ClientTaskModel>>>, IUserRequest;
