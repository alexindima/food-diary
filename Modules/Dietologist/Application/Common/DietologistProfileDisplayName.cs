using FoodDiary.Modules.Users.Contracts.Models;

namespace FoodDiary.Modules.Dietologist.Application.Common;

internal static class DietologistProfileDisplayName {
    public static string Resolve(UserDietologistProfileModel user) {
        string fullName = $"{user.FirstName} {user.LastName}".Trim();
        if (!string.IsNullOrWhiteSpace(fullName)) {
            return fullName;
        }
        return user.Email ?? (string.Equals(user.Language, "ru", StringComparison.Ordinal) ? "Пользователь" : "User");
    }
}
