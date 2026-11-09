using FoodDiary.Mediator;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Users.Contracts.Commands.SetUserManualPremiumGrant;

public sealed record SetUserManualPremiumGrantCommand(UserId UserId, DateTimeOffset? EndsAt, string? Reason)
    : IRequest<Result<UserManualPremiumGrantModel>>;
