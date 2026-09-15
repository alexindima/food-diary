namespace FoodDiary.Modules.Users.Contracts.Models;

public sealed record WeightGoalHistoryModel(
    Guid Id,
    double TargetWeightKg,
    double StartWeightKg,
    double? EndWeightKg,
    DateTime StartedAtUtc,
    DateTime? EndedAtUtc,
    string Status);
