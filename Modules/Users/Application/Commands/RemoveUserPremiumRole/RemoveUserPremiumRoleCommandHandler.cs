using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.Enums;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Contracts.Commands.RemoveUserPremiumRole;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.Users.Application.Commands.RemoveUserPremiumRole;

public sealed class RemoveUserPremiumRoleCommandHandler(IUserRoleMembershipService roleMembershipService) : IRequestHandler<RemoveUserPremiumRoleCommand, Unit> {
    public async Task<Unit> Handle(RemoveUserPremiumRoleCommand request, CancellationToken cancellationToken) {
        UserId userId = request.UserId;
        await roleMembershipService.RemoveRoleAsync(userId, RoleNames.Premium, cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }

}
