namespace FoodDiary.Application.Abstractions.Wearables.Models;

public sealed record WearableTokenResult(
    string AccessToken,
    string? RefreshToken,
    string ExternalUserId,
    DateTime? ExpiresAtUtc);
