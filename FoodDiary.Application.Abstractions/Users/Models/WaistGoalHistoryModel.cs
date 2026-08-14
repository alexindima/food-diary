namespace FoodDiary.Application.Abstractions.Users.Models;

public sealed record WaistGoalHistoryModel(
    Guid Id,
    double TargetWaistCm,
    double StartWaistCm,
    double? EndWaistCm,
    DateTime StartedAtUtc,
    DateTime? EndedAtUtc,
    string Status);
