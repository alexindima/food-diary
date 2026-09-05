namespace FoodDiary.Presentation.Api.Features.Admin.Responses;

public sealed record FastingTelemetryPresetHttpResponse(
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
