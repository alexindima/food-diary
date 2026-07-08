using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Results;
using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Users.Common;

public static class CurrentUserAccessPolicy {
    public static Error? EnsureCanAccess(User? user) {
        if (user is null) {
            return Errors.Authentication.InvalidToken;
        }

        if (user.DeletedAt is not null) {
            return Errors.Authentication.AccountDeleted;
        }

        if (!user.IsActive) {
            return Errors.Authentication.InvalidToken;
        }

        return null;
    }
}
