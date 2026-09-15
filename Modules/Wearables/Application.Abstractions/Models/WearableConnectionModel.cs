namespace FoodDiary.Modules.Wearables.Application.Abstractions.Models;

public sealed record WearableConnectionModel(
    string Provider,
    string ExternalUserId,
    bool IsActive,
    DateTime? LastSyncedAtUtc,
    DateTime ConnectedAtUtc);
