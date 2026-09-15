using FoodDiary.Modules.Users.Application.Abstractions.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Contracts.Queries.GetUserBillingProfileIncludingDeleted;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.Users.Application.Queries.GetUserBillingProfileIncludingDeleted;

public sealed class GetUserBillingProfileIncludingDeletedQueryHandler(IUserBillingProfileReadModelRepository repository) : IRequestHandler<GetUserBillingProfileIncludingDeletedQuery, UserBillingProfileModel?> {
    public Task<UserBillingProfileModel?> Handle(GetUserBillingProfileIncludingDeletedQuery request, CancellationToken cancellationToken) {
        UserId userId = request.UserId;
        return repository.GetBillingProfileIncludingDeletedAsync(userId, cancellationToken);
    }

}
