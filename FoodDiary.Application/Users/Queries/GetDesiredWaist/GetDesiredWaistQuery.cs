using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Users.Models;

namespace FoodDiary.Application.Users.Queries.GetDesiredWaist;

public record GetDesiredWaistQuery(Guid? UserId)
    : IQuery<Result<UserDesiredWaistModel>>, IUserRequest;
