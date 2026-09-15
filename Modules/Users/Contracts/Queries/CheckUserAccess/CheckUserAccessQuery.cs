using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.Users.Contracts.Queries.CheckUserAccess;

public sealed record CheckUserAccessQuery(
    UserId UserId) : IRequest<Error?>;
