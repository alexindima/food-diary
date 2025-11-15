using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Contracts.Users;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Users.Queries.GetDesiredWeight;

public record GetDesiredWeightQuery(UserId? UserId)
    : IQuery<Result<UserDesiredWeightResponse>>;
