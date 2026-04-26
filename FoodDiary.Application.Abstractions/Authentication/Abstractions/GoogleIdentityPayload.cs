namespace FoodDiary.Application.Abstractions.Authentication.Abstractions;

public sealed record GoogleIdentityPayload(
    string Email,
    string? FirstName,
    string? LastName,
    string? Locale);
