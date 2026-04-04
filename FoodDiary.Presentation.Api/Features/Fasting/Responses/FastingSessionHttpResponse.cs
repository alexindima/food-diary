namespace FoodDiary.Presentation.Api.Features.Fasting.Responses;

public sealed record FastingSessionHttpResponse(
    Guid Id,
    DateTime StartedAtUtc,
    DateTime? EndedAtUtc,
    int PlannedDurationHours,
    string Protocol,
    bool IsCompleted,
    string? Notes);
