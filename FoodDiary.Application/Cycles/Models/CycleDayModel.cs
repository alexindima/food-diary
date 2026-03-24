namespace FoodDiary.Application.Cycles.Models;

public sealed record CycleDayModel(
    Guid Id,
    Guid CycleId,
    DateTime Date,
    bool IsPeriod,
    DailySymptomsModel Symptoms,
    string? Notes);
