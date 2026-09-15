namespace FoodDiary.Modules.Marketing.Application.Abstractions.Common;

public interface IMarketingAttributionRangeReadRepository {
    Task<MarketingAttributionRangeRecord> GetRangeAsync(MarketingAttributionRangeFilter filter, CancellationToken cancellationToken);
}
