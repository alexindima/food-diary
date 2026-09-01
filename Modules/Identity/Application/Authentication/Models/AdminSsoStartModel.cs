namespace FoodDiary.Application.Identity.Authentication.Models;

public sealed record AdminSsoStartModel(
    string Code,
    DateTime ExpiresAtUtc);
