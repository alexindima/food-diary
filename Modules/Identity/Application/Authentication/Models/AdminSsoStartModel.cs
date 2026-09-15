namespace FoodDiary.Modules.Identity.Application.Authentication.Models;

public sealed record AdminSsoStartModel(
    string Code,
    DateTime ExpiresAtUtc);
