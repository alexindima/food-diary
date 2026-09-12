namespace FoodDiary.Application.Abstractions.Authentication.Abstractions;

public sealed record TelegramOidcIdentity(
    string Issuer,
    string Subject,
    long TelegramUserId,
    string? FirstName,
    string? LastName,
    string? Username);
