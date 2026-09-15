using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Modules.Images.Application.Abstractions.Common;
using FoodDiary.Modules.Images.Service.Contracts.Common;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Images.Infrastructure.Persistence.Images;

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
