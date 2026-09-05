namespace FoodDiary.Presentation.Api.Features.Cycles.Responses;

public sealed record CycleConsentHttpResponse(
    Guid Id,
    int Purpose,
    DateTime GrantedAtUtc,
    DateTime? RevokedAtUtc,
    bool IsActive);
