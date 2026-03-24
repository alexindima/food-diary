namespace FoodDiary.Presentation.Api.Features.Auth.Requests;

public sealed record TelegramLoginWidgetHttpRequest(
    long Id,
    long AuthDate,
    string Hash,
    string? Username,
    string? FirstName,
    string? LastName,
    string? PhotoUrl);
