namespace FoodDiary.Presentation.Api.Features.Admin.Responses;

public sealed record FastingTelemetrySummaryHttpResponse(
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
    DateTime? LastEventAtUtc,
    IReadOnlyList<FastingTelemetryPresetHttpResponse> TopPresets);
