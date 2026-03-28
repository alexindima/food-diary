using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Users.Models;

namespace FoodDiary.Application.Users.Queries.GetDesiredWaist;

public record GetDesiredWaistQuery(Guid? UserId)
    : IQuery<Result<UserDesiredWaistModel>>, IUserRequest;
