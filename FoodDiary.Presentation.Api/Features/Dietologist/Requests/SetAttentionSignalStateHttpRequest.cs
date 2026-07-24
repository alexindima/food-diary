namespace FoodDiary.Presentation.Api.Features.Dietologist.Requests;

public sealed record SetAttentionSignalStateHttpRequest(
    Guid ClientUserId,
    string Action,
    DateTime? SnoozedUntilUtc);
