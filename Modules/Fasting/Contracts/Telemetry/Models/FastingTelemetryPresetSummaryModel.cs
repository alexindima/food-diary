namespace FoodDiary.Modules.Fasting.Contracts.Telemetry.Models;

public sealed record FastingTelemetryPresetSummaryModel(
    string PresetId,
    int SelectionCount,
    int TimingSaveCount,
    int? FirstReminderHours,
    int? FollowUpReminderHours,
    int StartedSessions,
    int CompletedSessions,
    int SavedCheckIns,
    double CompletionRatePercent,
    double CheckInRatePercent);
