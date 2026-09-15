namespace FoodDiary.Modules.Marketing.Application.Abstractions.Common;

public interface IMarketingAttributionEventReadRepository {
    Task<MarketingAttributionSummaryRecord> GetSummaryAsync(DateTime sinceUtc, CancellationToken cancellationToken = default);

    Task<MarketingAttributionEventRecord?> GetLandingAsync(
        string anonymousId,
        string sessionId,
        DateTime sinceUtc,
        CancellationToken cancellationToken = default);

    Task<MarketingAttributionEventRecord?> GetLatestForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> ExistsForUserAsync(Guid userId, string eventType, CancellationToken cancellationToken = default);
}
