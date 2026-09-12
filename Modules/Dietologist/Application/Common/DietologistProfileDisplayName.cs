using FoodDiary.Application.Abstractions.Users.Models;

namespace FoodDiary.Application.Dietologist.Common;

internal static class DietologistProfileDisplayName {
    public static string Resolve(UserDietologistProfileModel user) {
        string fullName = $"{user.FirstName} {user.LastName}".Trim();
        if (!string.IsNullOrWhiteSpace(fullName)) {
            return fullName;
        }
        return user.Email ?? (string.Equals(user.Language, "ru", StringComparison.Ordinal) ? "Пользователь" : "User");
    }
}
