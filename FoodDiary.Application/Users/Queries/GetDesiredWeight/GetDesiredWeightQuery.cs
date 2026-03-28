using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Users.Models;

namespace FoodDiary.Application.Users.Queries.GetDesiredWeight;

public record GetDesiredWeightQuery(Guid? UserId)
    : IQuery<Result<UserDesiredWeightModel>>, IUserRequest;
