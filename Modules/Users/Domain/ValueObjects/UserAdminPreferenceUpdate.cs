namespace FoodDiary.Modules.Users.Domain.ValueObjects;

public readonly record struct UserAdminPreferenceUpdate(
    string? Language = null);
