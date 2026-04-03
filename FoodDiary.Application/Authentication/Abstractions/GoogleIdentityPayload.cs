namespace FoodDiary.Application.Authentication.Abstractions;

public sealed record GoogleIdentityPayload(
    string Email,
    string? FirstName,
    string? LastName,
    string? Locale);
