namespace FoodDiary.Application.Authentication.Models;

public sealed record AdminSsoStartModel(
    string Code,
    DateTime ExpiresAtUtc);
