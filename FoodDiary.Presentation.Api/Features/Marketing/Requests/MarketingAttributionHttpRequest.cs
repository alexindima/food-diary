namespace FoodDiary.Presentation.Api.Features.Marketing.Requests;

public sealed record MarketingAttributionHttpRequest(
    string EventType,
    string Timestamp,
    Guid? UserId,
    string AnonymousId,
    string SessionId,
    string LandingPath,
    string? ReferrerHost = null,
    string? UtmSource = null,
    string? UtmMedium = null,
    string? UtmCampaign = null,
    string? UtmContent = null,
    string? UtmTerm = null,
    string? BuildVersion = null);
