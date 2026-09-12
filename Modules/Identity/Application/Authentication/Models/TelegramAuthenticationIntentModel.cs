namespace FoodDiary.Application.Identity.Authentication.Models;

public sealed record TelegramAuthenticationIntentModel(string Ticket, string NextAction, DateTime ExpiresAtUtc);
