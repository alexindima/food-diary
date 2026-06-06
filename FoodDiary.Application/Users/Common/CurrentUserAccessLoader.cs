using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Users.Common;

public static class CurrentUserAccessLoader {
    public static async Task<Error?> EnsureCanAccessAsync(
        IUserRepository userRepository,
        UserId userId,
        CancellationToken cancellationToken) {
        User? user = await userRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        return CurrentUserAccessPolicy.EnsureCanAccess(user);
    }
}
