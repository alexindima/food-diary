namespace FoodDiary.Modules.Users.Domain.ValueObjects;

public readonly record struct UserPersonalInfoUpdate(
    string? Username = null,
    string? FirstName = null,
    string? LastName = null,
    DateTime? BirthDate = null,
    string? Gender = null,
    ProfileWeightKg? WeightKg = null,
    ProfileHeightCm? HeightCm = null,
    bool BirthDateSpecified = false);
