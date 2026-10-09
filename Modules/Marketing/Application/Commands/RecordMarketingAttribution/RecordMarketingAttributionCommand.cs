using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Marketing.Domain.ValueObjects;
using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Mediator;
using FoodDiary.Results;

namespace FoodDiary.Modules.Marketing.Application.Commands.RecordMarketingAttribution;

public sealed record RecordMarketingAttributionCommand(
    string EventType,
    string? Timestamp,
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
    Guid? EventId = null) : IRequest<Result>, ITransactionalCommand;
