using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.Users.Contracts.Commands.RemoveUserPremiumRole;

// Executes within the caller-owned unit of work; this request does not commit.
public sealed record RemoveUserPremiumRoleCommand(
    UserId UserId) : IRequest<Unit>;
