namespace FoodDiary.Domain.ValueObjects;

public readonly record struct UserPreferenceState(
    string? DashboardLayoutJson,
    string? Language) {
    public static UserPreferenceState CreateInitial() {
        return new UserPreferenceState(
            DashboardLayoutJson: null,
            Language: null);
    }
}
