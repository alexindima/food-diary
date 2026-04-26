using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Users.Models;

namespace FoodDiary.Application.Dietologist.Queries.GetClientGoals;

public record GetClientGoalsQuery(
    Guid? UserId,
    Guid ClientUserId) : IQuery<Result<UserModel>>, IUserRequest;
