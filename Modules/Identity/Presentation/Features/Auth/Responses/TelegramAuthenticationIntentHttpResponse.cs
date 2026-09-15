namespace FoodDiary.Modules.Identity.Presentation.Features.Auth.Responses;

public sealed record TelegramAuthenticationIntentHttpResponse(string Ticket, string NextAction, DateTime ExpiresAtUtc);
