using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.Users.Contracts.Commands.EnsureUserPremiumRole;

// Executes within the caller-owned unit of work; this request does not commit.
public sealed record EnsureUserPremiumRoleCommand(
    UserId UserId) : IRequest<Unit>;
