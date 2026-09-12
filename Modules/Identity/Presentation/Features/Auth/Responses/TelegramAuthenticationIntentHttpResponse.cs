namespace FoodDiary.Presentation.Api.Features.Auth.Responses;

public sealed record TelegramAuthenticationIntentHttpResponse(string Ticket, string NextAction, DateTime ExpiresAtUtc);
