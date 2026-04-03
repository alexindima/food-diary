using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Users.Common;

public static class CurrentUserAccessLoader {
    public static async Task<Error?> EnsureCanAccessAsync(
        IUserRepository userRepository,
        UserId userId,
        CancellationToken cancellationToken) {
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        return CurrentUserAccessPolicy.EnsureCanAccess(user);
    }
}
