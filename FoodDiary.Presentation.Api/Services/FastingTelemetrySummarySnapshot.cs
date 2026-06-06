namespace FoodDiary.Presentation.Api.Services;

public sealed record FastingTelemetrySummarySnapshot(
    int WindowHours,
    DateTime GeneratedAtUtc,
    int StartedSessions,
    int CompletedSessions,
    int SavedCheckIns,
    int ReminderPresetSelections,
    int ReminderTimingSaves,
    int PresetReminderTimingSaves,
    int ManualReminderTimingSaves,
    double CompletionRatePercent,
    double CheckInRatePercent,
    double? AverageCompletedDurationHours,
    DateTime? LastCheckInAtUtc,
    DateTime? LastEventAtUtc,
    IReadOnlyList<FastingTelemetryPresetSnapshot> TopPresets);
