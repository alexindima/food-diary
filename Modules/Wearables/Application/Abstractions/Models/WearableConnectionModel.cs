namespace FoodDiary.Application.Abstractions.Wearables.Models;

public sealed record WearableConnectionModel(
    string Provider,
    string ExternalUserId,
    bool IsActive,
    DateTime? LastSyncedAtUtc,
    DateTime ConnectedAtUtc);
