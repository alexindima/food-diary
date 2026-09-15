using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Users.Contracts.Models;

namespace FoodDiary.Modules.Users.Application.Queries.GetDesiredWaist;

public record GetDesiredWaistQuery(Guid? UserId)
    : IQuery<Result<UserDesiredWaistModel>>, IUserRequest;
