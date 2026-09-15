using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Results;
using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Users.Common;

public static class CurrentUserAccessPolicy {
    public static Error? EnsureCanAccess(User? user) {
        if (user is null) {
            return AuthenticationErrors.InvalidToken;
        }

        if (user.DeletedAt is not null) {
            return UserAuthenticationErrors.AccountDeleted;
        }

        if (!user.IsActive) {
            return AuthenticationErrors.InvalidToken;
        }

        return null;
    }
}
