using FoodDiary.Mediator;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Users.Contracts.Queries.GetUserRegistrationTime;

public sealed record GetUserRegistrationTimeQuery(UserId UserId) : IRequest<DateTime?>;
