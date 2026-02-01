namespace FoodDiary.Contracts.Authentication;

public sealed record TelegramLoginWidgetRequest(
    long Id,
    long AuthDate,
    string Hash,
    string? Username,
    string? FirstName,
    string? LastName,
    string? PhotoUrl);
