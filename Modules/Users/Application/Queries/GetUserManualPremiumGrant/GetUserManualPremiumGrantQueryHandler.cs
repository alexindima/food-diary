using FoodDiary.Mediator;
using FoodDiary.Modules.Users.Application.Abstractions.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Contracts.Queries.GetUserManualPremiumGrant;

namespace FoodDiary.Modules.Users.Application.Queries.GetUserManualPremiumGrant;

public sealed class GetUserManualPremiumGrantQueryHandler(IUserAdminReadModelRepository users)
    : IRequestHandler<GetUserManualPremiumGrantQuery, UserManualPremiumGrantModel?> {
    public Task<UserManualPremiumGrantModel?> Handle(GetUserManualPremiumGrantQuery request, CancellationToken cancellationToken) =>
        users.GetManualPremiumGrantAsync(request.UserId, cancellationToken);
}
