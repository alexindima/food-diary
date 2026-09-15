namespace FoodDiary.Modules.Marketing.Contracts.Models;

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
