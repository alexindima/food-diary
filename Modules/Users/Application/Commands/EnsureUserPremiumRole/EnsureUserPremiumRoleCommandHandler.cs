using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Commands.EnsureUserPremiumRole;
using FoodDiary.Mediator;

namespace FoodDiary.Application.Users.Commands.EnsureUserPremiumRole;

public sealed class EnsureUserPremiumRoleCommandHandler(IUserRoleMembershipService roleMembershipService) : IRequestHandler<EnsureUserPremiumRoleCommand, Unit> {
    public async Task<Unit> Handle(EnsureUserPremiumRoleCommand request, CancellationToken cancellationToken) {
        UserId userId = request.UserId;
        await roleMembershipService.EnsureRoleAsync(userId, RoleNames.Premium, cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }

}
