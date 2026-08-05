namespace FoodDiary.Application.Users.Models;

public sealed record WeightGoalHistoryModel(
    Guid Id,
    double TargetWeight,
    double StartWeight,
    double? EndWeight,
    DateTime StartedAtUtc,
    DateTime? EndedAtUtc,
    string Status);
