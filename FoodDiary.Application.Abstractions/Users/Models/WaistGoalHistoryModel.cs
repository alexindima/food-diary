namespace FoodDiary.Application.Abstractions.Users.Models;

public sealed record WaistGoalHistoryModel(
    Guid Id,
    double TargetWaist,
    double StartWaist,
    double? EndWaist,
    DateTime StartedAtUtc,
    DateTime? EndedAtUtc,
    string Status);
