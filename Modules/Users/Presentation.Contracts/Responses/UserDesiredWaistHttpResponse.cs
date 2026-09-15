namespace FoodDiary.Modules.Users.Presentation.Contracts.Responses;

public sealed record UserDesiredWaistHttpResponse(
    double? DesiredWaistCm,
    double? StartWaistCm,
    DateTime? StartedAtUtc);
