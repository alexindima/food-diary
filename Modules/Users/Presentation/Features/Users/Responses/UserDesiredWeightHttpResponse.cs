namespace FoodDiary.Presentation.Api.Features.Users.Responses;

public sealed record UserDesiredWeightHttpResponse(
    double? DesiredWeightKg,
    double? StartWeightKg = null,
    DateTime? StartedAtUtc = null);
