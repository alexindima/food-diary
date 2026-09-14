using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Mediator;

namespace FoodDiary.Application.Abstractions.Commands.RemoveUserPremiumRole;

// Executes within the caller-owned unit of work; this request does not commit.
public sealed record RemoveUserPremiumRoleCommand(
    UserId UserId) : IRequest<Unit>;
