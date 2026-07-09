namespace FoodDiary.Application.Abstractions.Marketing.Common;

public interface IMarketingAttributionEventReadRepository {
    Task<IReadOnlyList<MarketingAttributionEventRecord>> GetSinceAsync(DateTime sinceUtc, CancellationToken cancellationToken = default);

    Task<MarketingAttributionEventRecord?> GetLatestForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> ExistsForUserAsync(Guid userId, string eventType, CancellationToken cancellationToken = default);
}
