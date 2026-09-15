namespace FoodDiary.Modules.Users.Contracts.Models;

public sealed record UserDesiredWeightModel(
    double? DesiredWeightKg,
    double? StartWeightKg = null,
    DateTime? StartedAtUtc = null);
