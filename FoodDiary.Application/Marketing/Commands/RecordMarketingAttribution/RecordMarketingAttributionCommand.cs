using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Marketing.Commands.RecordMarketingAttribution;

public sealed record RecordMarketingAttributionCommand(
    string EventType,
    string? Timestamp,
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
    string? BuildVersion) : ICommand<Result>;
