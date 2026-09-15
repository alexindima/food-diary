namespace FoodDiary.Modules.Marketing.Application.Abstractions.Common;

public interface IMarketingAttributionEventWriteRepository {
    Task AddAsync(MarketingAttributionEventRecord record, CancellationToken cancellationToken = default);

    Task<int> DeleteOlderThanAsync(
        DateTime olderThanUtc,
        int batchSize,
        CancellationToken cancellationToken = default);
}
