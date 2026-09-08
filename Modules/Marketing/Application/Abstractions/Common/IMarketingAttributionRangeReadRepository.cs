namespace FoodDiary.Application.Abstractions.Marketing.Common;

public interface IMarketingAttributionRangeReadRepository {
    Task<MarketingAttributionRangeRecord> GetRangeAsync(MarketingAttributionRangeFilter filter, CancellationToken cancellationToken);
}
