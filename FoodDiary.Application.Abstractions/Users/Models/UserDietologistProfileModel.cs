namespace FoodDiary.Application.Abstractions.Users.Models;

public sealed record UserDietologistProfileModel(
    Guid Id,
    string Email,
    string? FirstName,
    string? LastName,
    string? Language,
    bool IsDietologist);
