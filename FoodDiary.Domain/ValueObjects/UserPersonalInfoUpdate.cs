namespace FoodDiary.Domain.ValueObjects;

public readonly record struct UserPersonalInfoUpdate(
    string? Username = null,
    string? FirstName = null,
    string? LastName = null,
    DateTime? BirthDate = null,
    string? Gender = null,
    double? Weight = null,
    double? Height = null);
