namespace FoodDiary.Application.Fasting.Models;

public sealed record FastingSessionModel(
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
