namespace FoodDiary.Application.Abstractions.Users.Models;

public sealed record UserDesiredWeightModel(
    double? DesiredWeight,
    double? StartWeight = null,
    DateTime? StartedAtUtc = null);
