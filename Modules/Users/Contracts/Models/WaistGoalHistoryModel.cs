namespace FoodDiary.Modules.Users.Contracts.Models;

public sealed record WaistGoalHistoryModel(
    Guid Id,
    double TargetWaistCm,
    double StartWaistCm,
    double? EndWaistCm,
    DateTime StartedAtUtc,
    DateTime? EndedAtUtc,
    string Status);
