namespace FoodDiary.Application.Marketing.Models;

public sealed record MarketingAttributionRecentEventModel(
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
