namespace FoodDiary.Presentation.Api.Services;

public sealed record FastingTelemetryPresetSnapshot(
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
