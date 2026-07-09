namespace FoodDiary.Application.Abstractions.Marketing.Common;

public sealed record MarketingAttributionEventRecord(
    string EventType,
    DateTime OccurredAtUtc,
    Guid? UserId,
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
