namespace FoodDiary.Modules.Dietologist.Presentation.Responses;

public sealed record AttentionSignalHttpResponse(
    string Id,
    Guid ClientUserId,
    string? ClientDisplayName,
    string Type,
    string Severity,
    string Reason,
    DateTime DetectedAtUtc,
    DateTime? SnoozedUntilUtc);
