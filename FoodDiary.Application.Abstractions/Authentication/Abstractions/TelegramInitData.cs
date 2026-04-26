namespace FoodDiary.Application.Abstractions.Authentication.Abstractions;

public sealed record TelegramInitData(
    long UserId,
    string? Username,
    string? FirstName,
    string? LastName,
    string? PhotoUrl,
    string? LanguageCode,
    DateTime AuthDateUtc
);
