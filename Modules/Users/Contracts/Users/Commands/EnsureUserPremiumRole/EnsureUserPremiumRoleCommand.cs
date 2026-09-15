using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Mediator;

namespace FoodDiary.Application.Abstractions.Users.Commands.EnsureUserPremiumRole;

// Executes within the caller-owned unit of work; this request does not commit.
public sealed record EnsureUserPremiumRoleCommand(
    UserId UserId) : IRequest<Unit>;
