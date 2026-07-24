using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Dietologist.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.Dietologist.Queries.GetClientTasksForDietologist;

public sealed record GetClientTasksForDietologistQuery(
    Guid? UserId,
    Guid ClientUserId) : IQuery<Result<IReadOnlyList<ClientTaskModel>>>, IUserRequest;
