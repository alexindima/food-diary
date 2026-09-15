using FoodDiary.Mediator;
using FoodDiary.Modules.Marketing.Contracts.Commands.CleanupMarketingAttribution;
using FoodDiary.Modules.Marketing.Application.Abstractions.Common;

namespace FoodDiary.Modules.Marketing.Application.Commands.CleanupMarketingAttribution;

public sealed class CleanupMarketingAttributionCommandHandler(IMarketingAttributionEventWriteRepository repository)
    : IRequestHandler<CleanupMarketingAttributionCommand, int> {
    public async Task<int> Handle(CleanupMarketingAttributionCommand request, CancellationToken cancellationToken) {
        DateTime olderThanUtc = request.OlderThanUtc;
        int batchSize = request.BatchSize;
        if (batchSize <= 0) {
            return 0;
        }
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
