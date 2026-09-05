namespace FoodDiary.Presentation.Api.Features.Wearables.Responses;

public sealed record WearableConnectionHttpResponse(
    string Provider,
    string ExternalUserId,
    bool IsActive,
    DateTime? LastSyncedAtUtc,
    DateTime ConnectedAtUtc);
