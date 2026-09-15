namespace FoodDiary.Modules.Wearables.Presentation.Responses;

public sealed record WearableConnectionHttpResponse(
    string Provider,
    string ExternalUserId,
    bool IsActive,
    DateTime? LastSyncedAtUtc,
    DateTime ConnectedAtUtc);
