using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.Persistence.Images;

internal sealed class ImageAssetCleanupBatch(IServiceScopeFactory scopeFactory) : IImageAssetCleanupBatch {
    public async Task<bool> DeleteUnusedAsync(ImageAssetId assetId, CancellationToken cancellationToken = default) {
        AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        await using (scope.ConfigureAwait(false)) {
            IImageAssetCleanupService cleanup = scope.ServiceProvider.GetRequiredService<IImageAssetCleanupService>();
            DeleteImageAssetResult result = await cleanup.DeleteIfUnusedAsync(assetId, cancellationToken).ConfigureAwait(false);
            if (!result.Deleted) { return false; }
            IUnitOfWork unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return true;
        }
    }
}
