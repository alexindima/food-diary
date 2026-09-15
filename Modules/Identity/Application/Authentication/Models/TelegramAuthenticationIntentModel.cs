namespace FoodDiary.Modules.Identity.Application.Authentication.Models;

public sealed record TelegramAuthenticationIntentModel(string Ticket, string NextAction, DateTime ExpiresAtUtc);
