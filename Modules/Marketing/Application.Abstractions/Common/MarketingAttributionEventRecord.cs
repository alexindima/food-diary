using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Marketing.Domain.ValueObjects;
namespace FoodDiary.Modules.Marketing.Application.Abstractions.Common;

public sealed record MarketingAttributionEventRecord(
    string EventType,
    DateTime OccurredAtUtc,
    UserId? UserId,
    AnonymousVisitorId AnonymousId,
    MarketingSessionId SessionId,
    string LandingPath,
    string? ReferrerHost,
    string? UtmSource,
    string? UtmMedium,
    string? UtmCampaign,
    string? UtmContent,
    string? UtmTerm,
    string? BuildVersion,
    Guid? EventId = null);
