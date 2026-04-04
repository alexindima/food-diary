namespace FoodDiary.Application.Fasting.Models;

public sealed record FastingSessionModel(
    Guid Id,
    DateTime StartedAtUtc,
    DateTime? EndedAtUtc,
    int PlannedDurationHours,
    string Protocol,
    bool IsCompleted,
    string? Notes);
