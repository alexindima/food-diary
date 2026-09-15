using FoodDiary.Modules.Images.Application.Abstractions.Common;
using FoodDiary.Modules.Images.Application.Abstractions.Models;
using Microsoft.Extensions.Logging;
using FoodDiary.Mediator;
using FoodDiary.Modules.Images.Service.Contracts.Commands.CleanupOrphanImages;

namespace FoodDiary.Modules.Images.Application.Commands.CleanupOrphanImages;

public sealed class CleanupOrphanImagesCommandHandler(IImageAssetUsageQuery usageQuery, IImageAssetCleanupBatch cleanupBatch, ILogger<CleanupOrphanImagesCommandHandler> logger) : IRequestHandler<CleanupOrphanImagesCommand, int> {
    public async Task<int> Handle(CleanupOrphanImagesCommand request, CancellationToken cancellationToken) {
        DateTime olderThanUtc = request.OlderThanUtc;
        int batchSize = request.BatchSize;
        if (batchSize <= 0) {
            return 0;
        }

        DateTime normalizedOlderThanUtc = olderThanUtc.Kind switch {
            DateTimeKind.Utc => olderThanUtc,
            _ => olderThanUtc.ToUniversalTime(),
        };

        int removed = 0;
        ImageCleanupCandidate? after = null;
        while (true) {
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyList<ImageCleanupCandidate> candidates = await usageQuery
                .GetUnusedCandidatesOlderThanAsync(normalizedOlderThanUtc, batchSize, after, cancellationToken).ConfigureAwait(false);
            if (candidates.Count == 0) {
                break;
            }
            foreach (ImageCleanupCandidate candidate in candidates) {
                try {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (await cleanupBatch.DeleteUnusedAsync(candidate.Id, cancellationToken).ConfigureAwait(false)) {
                        removed++;
                    }
                } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
                    throw;
                } catch (Exception ex) {
                    logger.LogWarning(ex, "Failed to remove orphan image asset {AssetId}", candidate.Id);
                }
            }
            after = candidates[^1];
        }

        return removed;
    }
}
