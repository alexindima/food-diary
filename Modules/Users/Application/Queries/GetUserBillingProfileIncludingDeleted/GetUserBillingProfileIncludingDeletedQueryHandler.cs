using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Users.Queries.GetUserBillingProfileIncludingDeleted;
using FoodDiary.Mediator;

namespace FoodDiary.Application.Users.Queries.GetUserBillingProfileIncludingDeleted;

public sealed class GetUserBillingProfileIncludingDeletedQueryHandler(IUserBillingProfileReadModelRepository repository) : IRequestHandler<GetUserBillingProfileIncludingDeletedQuery, UserBillingProfileModel?> {
    public Task<UserBillingProfileModel?> Handle(GetUserBillingProfileIncludingDeletedQuery request, CancellationToken cancellationToken) {
        UserId userId = request.UserId;
        return repository.GetBillingProfileIncludingDeletedAsync(userId, cancellationToken);
    }

}
