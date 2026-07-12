using FoodDiary.Application.Abstractions.Marketing.Common;
using FoodDiary.Application.Marketing.Common;

namespace FoodDiary.Application.Marketing.Services;

public sealed class MarketingAttributionCleanupService(IMarketingAttributionEventWriteRepository repository)
    : IMarketingAttributionCleanupService {
    public async Task<int> CleanupAsync(
        DateTime olderThanUtc,
        int batchSize,
        CancellationToken cancellationToken) {
        int totalDeletedCount = 0;
        int deletedCount;
        do {
            cancellationToken.ThrowIfCancellationRequested();
            deletedCount = await repository
                .DeleteOlderThanAsync(olderThanUtc, batchSize, cancellationToken)
                .ConfigureAwait(false);
            totalDeletedCount += deletedCount;
        } while (deletedCount == batchSize);

        return totalDeletedCount;
    }
}
