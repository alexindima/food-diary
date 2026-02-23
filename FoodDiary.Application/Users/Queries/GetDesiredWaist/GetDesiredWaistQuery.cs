using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Contracts.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Users.Queries.GetDesiredWaist;

public record GetDesiredWaistQuery(UserId? UserId)
    : IQuery<Result<UserDesiredWaistResponse>>;
