using FoodDiary.Modules.Users.Domain.Contracts.Enums;

namespace FoodDiary.Modules.Users.Domain.ValueObjects;

public readonly record struct UserPersonalProfileState(
    string? Username,
    string? FirstName,
    string? LastName,
    DateTime? BirthDate,
    string? Gender,
    double? WeightKg,
    double? HeightCm,
    ActivityLevel ActivityLevel) {
    public static UserPersonalProfileState CreateInitial() {
        return new UserPersonalProfileState(
            Username: null,
            FirstName: null,
            LastName: null,
            BirthDate: null,
            Gender: null,
            WeightKg: null,
            HeightCm: null,
            ActivityLevel: ActivityLevel.Moderate);
    }
}
