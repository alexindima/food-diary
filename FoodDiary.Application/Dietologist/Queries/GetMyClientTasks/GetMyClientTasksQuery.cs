using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.Dietologist.Queries.GetMyClientTasks;

public sealed record GetMyClientTasksQuery(Guid? UserId)
    : IQuery<Result<IReadOnlyList<ClientTaskModel>>>, IUserRequest;
