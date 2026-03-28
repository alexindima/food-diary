namespace FoodDiary.Domain.ValueObjects;

public readonly record struct UserRefreshTokenUpdate(
    string? RefreshToken,
    DateTime? ChangedAtUtc = null);
