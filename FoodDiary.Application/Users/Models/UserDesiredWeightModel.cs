namespace FoodDiary.Application.Users.Models;

public sealed record UserDesiredWeightModel(
    double? DesiredWeight,
    double? StartWeight = null,
    DateTime? StartedAtUtc = null);
