namespace FoodDiary.Modules.Users.Contracts.Models;

public sealed record UserDesiredWaistModel(
    double? DesiredWaistCm,
    double? StartWaistCm = null,
    DateTime? StartedAtUtc = null);
