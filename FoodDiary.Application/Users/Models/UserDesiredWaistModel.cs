namespace FoodDiary.Application.Users.Models;

public sealed record UserDesiredWaistModel(
    double? DesiredWaist,
    double? StartWaist = null,
    DateTime? StartedAtUtc = null);
