using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Queries.CheckUserAccess;
using FoodDiary.Mediator;

namespace FoodDiary.Application.Users.Queries.CheckUserAccess;

public sealed class CheckUserAccessQueryHandler(IUserLookupRepository userLookupRepository) : IRequestHandler<CheckUserAccessQuery, Error?> {
    public async Task<Error?> Handle(CheckUserAccessQuery request, CancellationToken cancellationToken) {
        UserId userId = request.UserId;
        User? user = await userLookupRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        return CurrentUserAccessPolicy.EnsureCanAccess(user);

    }

}
