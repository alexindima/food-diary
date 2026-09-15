using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.Enums;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Contracts.Commands.EnsureUserPremiumRole;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.Users.Application.Commands.EnsureUserPremiumRole;

public sealed class EnsureUserPremiumRoleCommandHandler(IUserRoleMembershipService roleMembershipService) : IRequestHandler<EnsureUserPremiumRoleCommand, Unit> {
    public async Task<Unit> Handle(EnsureUserPremiumRoleCommand request, CancellationToken cancellationToken) {
        UserId userId = request.UserId;
        await roleMembershipService.EnsureRoleAsync(userId, RoleNames.Premium, cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }

}
