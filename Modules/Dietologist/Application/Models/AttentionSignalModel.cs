namespace FoodDiary.Application.Dietologist.Models;

public sealed record AttentionSignalModel(
    string Id,
    Guid ClientUserId,
    string ClientDisplayName,
    string Type,
    string Severity,
    string Reason,
    DateTime DetectedAtUtc,
    DateTime? SnoozedUntilUtc);
