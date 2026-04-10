namespace FoodDiary.Application.Fasting.Common;

public sealed record FastingTelemetryEventRecord(
    string Name,
    DateTime OccurredAtUtc,
    string? SessionId,
    string? Protocol,
    string? PlanType,
    string? Status,
    string? OccurrenceKind,
    string? ReminderPresetId,
    string? ReminderSource,
    int? FirstReminderHours,
    int? FollowUpReminderHours,
    int? PlannedDurationHours,
    double? ActualDurationHours,
    int? HungerLevel,
    int? EnergyLevel,
    int? MoodLevel,
    int? SymptomsCount,
    bool? HadNotes);
