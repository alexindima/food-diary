using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Dietologist.Application.Models;

namespace FoodDiary.Modules.Dietologist.Application.Queries.GetClientGoals;

public record GetClientGoalsQuery(
    Guid? UserId,
    Guid ClientUserId) : IQuery<Result<ClientGoalsModel>>, IUserRequest;
