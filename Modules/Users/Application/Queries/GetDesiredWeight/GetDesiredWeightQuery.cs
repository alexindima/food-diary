using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Users.Contracts.Models;

namespace FoodDiary.Modules.Users.Application.Queries.GetDesiredWeight;

public record GetDesiredWeightQuery(Guid? UserId)
    : IQuery<Result<UserDesiredWeightModel>>, IUserRequest;
