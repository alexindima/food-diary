namespace FoodDiary.Application.Abstractions.Marketing.Common;

public interface IMarketingAttributionEventWriteRepository {
    Task AddAsync(MarketingAttributionEventRecord record, CancellationToken cancellationToken = default);

    Task<int> DeleteOlderThanAsync(
        DateTime olderThanUtc,
        int batchSize,
        CancellationToken cancellationToken = default);
}
