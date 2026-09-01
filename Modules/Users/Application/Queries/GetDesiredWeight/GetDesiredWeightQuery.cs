using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Models;

namespace FoodDiary.Application.Users.Queries.GetDesiredWeight;

public record GetDesiredWeightQuery(Guid? UserId)
    : IQuery<Result<UserDesiredWeightModel>>, IUserRequest;
