namespace FoodDiary.Application.Abstractions.Users.Models;

public sealed record UserTelegramRegistrationModel(
    long TelegramUserId,
    string? FirstName,
    string? LastName,
    string? Language,
    string TimeZoneId,
    DateTime RegisteredAtUtc,
    string? OidcIssuer = null,
    string? OidcSubject = null);
