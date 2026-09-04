namespace FoodDiary.Application.Abstractions.Users.Models;

public sealed record UserGoogleAuthenticationModel(
    string Issuer,
    string Subject,
    string Email,
    string? FirstName,
    string? LastName,
    string? Locale);
