namespace FoodDiary.Presentation.Api.Features.Users.Responses;

public sealed record UserDesiredWaistHttpResponse(
    double? DesiredWaistCm,
    double? StartWaistCm,
    DateTime? StartedAtUtc);
