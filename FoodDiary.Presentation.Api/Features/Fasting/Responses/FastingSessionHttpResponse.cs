namespace FoodDiary.Presentation.Api.Features.Fasting.Responses;

public sealed record FastingSessionHttpResponse(
    Guid Id,
    DateTime StartedAtUtc,
    DateTime? EndedAtUtc,
    int InitialPlannedDurationHours,
    int AddedDurationHours,
    int PlannedDurationHours,
    string Protocol,
    string PlanType,
    string OccurrenceKind,
    int? CyclicFastDays,
    int? CyclicEatDays,
    int? CyclicEatDayFastHours,
    int? CyclicEatDayEatingWindowHours,
    bool IsCompleted,
    string Status,
    string? Notes);
