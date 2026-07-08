using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Users.Models;

namespace FoodDiary.Application.Users.Queries.GetDesiredWeight;

public record GetDesiredWeightQuery(Guid? UserId)
    : IQuery<Result<UserDesiredWeightModel>>, IUserRequest;
