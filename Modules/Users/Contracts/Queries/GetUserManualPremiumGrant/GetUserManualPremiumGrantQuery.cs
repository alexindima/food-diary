using FoodDiary.Mediator;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Users.Contracts.Queries.GetUserManualPremiumGrant;

public sealed record GetUserManualPremiumGrantQuery(UserId UserId) : IRequest<UserManualPremiumGrantModel?>;
