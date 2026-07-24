namespace FoodDiary.Presentation.Api.Features.Dietologist.Responses;

public sealed record AttentionSignalHttpResponse(
    string Id,
    Guid ClientUserId,
    string ClientDisplayName,
    string Type,
    string Severity,
    string Reason,
    DateTime DetectedAtUtc,
    DateTime? SnoozedUntilUtc);
