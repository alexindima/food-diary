namespace FoodDiary.Domain.ValueObjects;

public readonly record struct UserPreferenceUpdate(
    string? DashboardLayoutJson = null,
    string? Language = null);
