namespace FoodDiary.Application.Abstractions.Users.Models;

public sealed record UserDesiredWeightModel(
    double? DesiredWeightKg,
    double? StartWeightKg = null,
    DateTime? StartedAtUtc = null);
