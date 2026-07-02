using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Users.Common;

public sealed class CurrentUserAccessService(IUserRepository userRepository) : ICurrentUserAccessService {
    public async Task<Error?> EnsureCanAccessAsync(UserId userId, CancellationToken cancellationToken = default) {
        User? user = await userRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        return CurrentUserAccessPolicy.EnsureCanAccess(user);
    }
}
