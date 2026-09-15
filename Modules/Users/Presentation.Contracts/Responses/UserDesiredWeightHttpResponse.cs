namespace FoodDiary.Modules.Users.Presentation.Contracts.Responses;

public sealed record UserDesiredWeightHttpResponse(
    double? DesiredWeightKg,
    double? StartWeightKg = null,
    DateTime? StartedAtUtc = null);
