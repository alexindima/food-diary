namespace FoodDiary.Application.Marketing.Common;

public interface IMarketingAttributionCleanupService {
    Task<int> CleanupAsync(
        DateTime olderThanUtc,
        int batchSize,
        CancellationToken cancellationToken);
}
