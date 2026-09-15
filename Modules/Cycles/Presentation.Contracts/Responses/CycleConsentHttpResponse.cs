namespace FoodDiary.Modules.Cycles.Presentation.Contracts.Responses;

public sealed record CycleConsentHttpResponse(
    Guid Id,
    int Purpose,
    DateTime GrantedAtUtc,
    DateTime? RevokedAtUtc,
    bool IsActive);
