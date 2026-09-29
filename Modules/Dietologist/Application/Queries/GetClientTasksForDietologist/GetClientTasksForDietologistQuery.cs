using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Dietologist.Application.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Dietologist.Application.Queries.GetClientTasksForDietologist;

public sealed record GetClientTasksForDietologistQuery(
    Guid? UserId,
    Guid ClientUserId,
    int Page = 1,
    int Limit = 50) : IQuery<Result<IReadOnlyList<ClientTaskModel>>>, IUserRequest;
