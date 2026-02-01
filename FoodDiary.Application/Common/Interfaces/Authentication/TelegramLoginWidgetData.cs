namespace FoodDiary.Application.Common.Interfaces.Authentication;

public sealed record TelegramLoginWidgetData(
    long Id,
    long AuthDate,
    string Hash,
    string? Username,
    string? FirstName,
    string? LastName,
    string? PhotoUrl);
