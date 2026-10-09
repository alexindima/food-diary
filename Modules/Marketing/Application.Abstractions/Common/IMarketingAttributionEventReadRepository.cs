using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Marketing.Domain.ValueObjects;
namespace FoodDiary.Modules.Marketing.Application.Abstractions.Common;

public interface IMarketingAttributionEventReadRepository {
    Task<MarketingAttributionSummaryRecord> GetSummaryAsync(DateTime sinceUtc, CancellationToken cancellationToken = default);

    Task<MarketingAttributionEventRecord?> GetLandingAsync(
        AnonymousVisitorId anonymousId,
        MarketingSessionId sessionId,
        DateTime sinceUtc,
        CancellationToken cancellationToken = default);

    Task<MarketingAttributionEventRecord?> GetLatestForUserAsync(UserId userId, CancellationToken cancellationToken = default);

    Task<bool> ExistsForUserAsync(UserId userId, string eventType, CancellationToken cancellationToken = default);
}
