namespace FoodDiary.Application.Abstractions.Users.Models;

public sealed record UserDesiredWaistModel(
    double? DesiredWaistCm,
    double? StartWaistCm = null,
    DateTime? StartedAtUtc = null);
