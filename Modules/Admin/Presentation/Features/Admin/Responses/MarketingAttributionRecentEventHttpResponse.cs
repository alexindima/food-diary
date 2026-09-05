namespace FoodDiary.Presentation.Api.Features.Admin.Responses;

public sealed record MarketingAttributionRecentEventHttpResponse(
    DateTime OccurredAtUtc,
    string EventType,
    string AnonymousId,
    string SessionId,
    string LandingPath,
    string? ReferrerHost,
    string? UtmSource,
    string? UtmMedium,
    string? UtmCampaign,
    string? UtmContent,
    string? UtmTerm,
    string? BuildVersion);
