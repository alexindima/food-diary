namespace FoodDiary.Domain.ValueObjects;

public readonly record struct UserAdminPreferenceUpdate(
    string? Language = null);
