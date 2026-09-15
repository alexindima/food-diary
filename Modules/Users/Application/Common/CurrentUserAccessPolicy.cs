using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Results;
using FoodDiary.Modules.Users.Domain.Entities;

namespace FoodDiary.Modules.Users.Application.Common;

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
