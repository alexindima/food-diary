using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Users.Commands.RemoveUserPremiumRole;
using FoodDiary.Mediator;

namespace FoodDiary.Application.Users.Commands.RemoveUserPremiumRole;

public sealed class RemoveUserPremiumRoleCommandHandler(IUserRoleMembershipService roleMembershipService) : IRequestHandler<RemoveUserPremiumRoleCommand, Unit> {
    public async Task<Unit> Handle(RemoveUserPremiumRoleCommand request, CancellationToken cancellationToken) {
        UserId userId = request.UserId;
        await roleMembershipService.RemoveRoleAsync(userId, RoleNames.Premium, cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }

}
