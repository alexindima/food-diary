namespace FoodDiary.Presentation.Api.Features.Users.Responses;

public sealed record UserDesiredWeightHttpResponse(
    double? DesiredWeight,
    double? StartWeight = null,
    DateTime? StartedAtUtc = null);
