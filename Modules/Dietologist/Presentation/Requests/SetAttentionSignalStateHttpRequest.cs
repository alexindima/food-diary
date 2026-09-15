namespace FoodDiary.Modules.Dietologist.Presentation.Requests;

public sealed record SetAttentionSignalStateHttpRequest(
    Guid ClientUserId,
    string Action,
    DateTime? SnoozedUntilUtc);
