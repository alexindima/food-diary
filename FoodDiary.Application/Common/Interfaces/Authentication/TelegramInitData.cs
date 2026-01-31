namespace FoodDiary.Application.Common.Interfaces.Authentication;

public sealed record TelegramInitData(
    long UserId,
    string? Username,
    string? FirstName,
    string? LastName,
    string? PhotoUrl,
    string? LanguageCode,
    DateTime AuthDateUtc
);
