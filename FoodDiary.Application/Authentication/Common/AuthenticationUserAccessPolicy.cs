using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Authentication.Common;

public static class AuthenticationUserAccessPolicy {
    public static Error? EnsureCanAuthenticate(User? user) {
        if (user is null) {
            return Errors.Authentication.InvalidCredentials;
        }

        if (user.DeletedAt is not null) {
            return Errors.Authentication.AccountDeleted;
        }

        if (!user.IsActive) {
            return Errors.Authentication.InvalidCredentials;
        }

        return null;
    }

    public static bool CanRequestPasswordReset(User? user) {
        return user is { IsActive: true, DeletedAt: null };
    }
}
