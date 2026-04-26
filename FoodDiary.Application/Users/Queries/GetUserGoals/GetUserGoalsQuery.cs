using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Users.Models;

namespace FoodDiary.Application.Users.Queries.GetUserGoals;

public record GetUserGoalsQuery(
    Guid? UserId
) : IQuery<Result<GoalsModel>>, IUserRequest;
