using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Models;

namespace FoodDiary.Application.Users.Queries.GetUserGoals;

public record GetUserGoalsQuery(
    Guid? UserId
) : IQuery<Result<GoalsModel>>, IUserRequest;
