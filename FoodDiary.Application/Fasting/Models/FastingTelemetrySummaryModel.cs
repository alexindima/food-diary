namespace FoodDiary.Application.Fasting.Models;

public sealed record FastingTelemetrySummaryModel(
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
    IReadOnlyList<FastingTelemetryPresetSummaryModel> TopPresets);
