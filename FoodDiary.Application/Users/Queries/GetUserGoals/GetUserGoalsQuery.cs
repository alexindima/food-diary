using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Contracts.Goals;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Users.Queries.GetUserGoals;

public record GetUserGoalsQuery(
    UserId? UserId
) : IQuery<Result<GoalsResponse>>;
