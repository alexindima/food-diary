using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Dietologist.Application.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Dietologist.Application.Queries.GetClientTasksForDietologist;

public sealed record GetClientTasksForDietologistQuery(
    Guid? UserId,
    Guid ClientUserId) : IQuery<Result<IReadOnlyList<ClientTaskModel>>>, IUserRequest;
