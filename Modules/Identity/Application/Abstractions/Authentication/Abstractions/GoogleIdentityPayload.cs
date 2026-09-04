namespace FoodDiary.Application.Abstractions.Authentication.Abstractions;

public sealed record GoogleIdentityPayload(
    string Issuer,
    string Subject,
    string Email,
    string? FirstName,
    string? LastName,
    string? Locale);
