using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Users.Contracts.Models;

namespace FoodDiary.Modules.Users.Application.Queries.GetUserGoals;

public record GetUserGoalsQuery(
    Guid? UserId
) : IQuery<Result<GoalsModel>>, IUserRequest;
