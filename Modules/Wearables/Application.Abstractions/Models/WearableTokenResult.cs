namespace FoodDiary.Modules.Wearables.Application.Abstractions.Models;

public sealed record WearableTokenResult(
    string AccessToken,
    string? RefreshToken,
    string ExternalUserId,
    DateTime? ExpiresAtUtc);
