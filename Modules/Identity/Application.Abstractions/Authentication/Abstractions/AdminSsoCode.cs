namespace FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;

public sealed record AdminSsoCode(string Code, DateTime ExpiresAtUtc);
