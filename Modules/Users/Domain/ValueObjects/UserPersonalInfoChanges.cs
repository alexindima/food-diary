using FoodDiary.Domain.Primitives;

namespace FoodDiary.Modules.Users.Domain.ValueObjects;

public sealed record UserPersonalInfoChanges(
    string? Username,
    string? FirstName,
    string? LastName,
    FieldChange<ProfileBirthDate> BirthDate,
    string? Gender,
    ProfileWeightKg? WeightKg,
    ProfileHeightCm? HeightCm);
