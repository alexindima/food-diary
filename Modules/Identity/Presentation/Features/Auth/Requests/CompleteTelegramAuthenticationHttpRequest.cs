namespace FoodDiary.Presentation.Api.Features.Auth.Requests;

public sealed record CompleteTelegramAuthenticationHttpRequest(string Ticket, string Action, string? Language = null, string? TimeZoneId = null);
