namespace FoodDiary.Contracts.Authentication;

public sealed record AdminSsoStartResponse(
    string Code,
    DateTime ExpiresAtUtc
);
