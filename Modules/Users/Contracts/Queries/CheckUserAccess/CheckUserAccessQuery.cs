using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;
using FoodDiary.Mediator;

namespace FoodDiary.Application.Abstractions.Queries.CheckUserAccess;

public sealed record CheckUserAccessQuery(
    UserId UserId) : IRequest<Error?>;
