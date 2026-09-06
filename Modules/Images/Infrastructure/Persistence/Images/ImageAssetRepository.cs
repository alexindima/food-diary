using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Images;

public sealed class ImageAssetRepository(FoodDiaryDbContext context) : IImageAssetRepository {
    public Task<ImageAsset> AddAsync(ImageAsset asset, CancellationToken cancellationToken = default) {
        context.ImageAssets.Add(asset);
        return Task.FromResult(asset);
    }

    public async Task<ImageAsset?> GetByIdAsync(ImageAssetId id, CancellationToken cancellationToken = default) {
        return await context.ImageAssets.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ImageAsset?> GetOwnedByIdAsync(ImageAssetId id, UserId userId, CancellationToken cancellationToken = default) {
        return await context.ImageAssets.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<ImageAsset?> GetOwnedForUpdateAsync(ImageAssetId id, UserId userId, CancellationToken cancellationToken = default) {
        return await context.ImageAssets
            .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId, cancellationToken)
            .ConfigureAwait(false);
    }

    public Task DeleteAsync(ImageAsset asset, CancellationToken cancellationToken = default) {
        context.ImageAssets.Attach(asset);
        context.ImageAssets.Remove(asset);
        return Task.CompletedTask;
    }

    public async Task<bool> IsAssetInUseAsync(ImageAssetId assetId, CancellationToken cancellationToken = default) {
        return await context.ImageAssets
            .Where(a => a.Id == assetId)
            .Select(_ =>
                context.Products.AsNoTracking().Any(p => p.ImageAssetId == assetId) ||
                context.Recipes.AsNoTracking().Any(r => r.ImageAssetId == assetId) ||
                context.RecipeSteps.AsNoTracking().Any(s => s.ImageAssetId == assetId) ||
                context.Meals.AsNoTracking().Any(m => m.ImageAssetId == assetId) ||
                context.MealAiSessions.AsNoTracking().Any(s => s.ImageAssetId == assetId) ||
                context.Users.AsNoTracking().Any(u => u.ProfileImageAssetId == assetId))
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ImageAsset>> GetUnusedOlderThanAsync(
        DateTime olderThanUtc,
        int batchSize,
        CancellationToken cancellationToken = default) {
        return await context.ImageAssets
            .AsNoTracking()
            .Where(asset =>
                asset.CreatedOnUtc < olderThanUtc &&
                !context.Products.AsNoTracking().Any(p => p.ImageAssetId == asset.Id) &&
                !context.Recipes.AsNoTracking().Any(r => r.ImageAssetId == asset.Id) &&
                !context.RecipeSteps.AsNoTracking().Any(s => s.ImageAssetId == asset.Id) &&
                !context.Meals.AsNoTracking().Any(m => m.ImageAssetId == asset.Id) &&
                !context.MealAiSessions.AsNoTracking().Any(s => s.ImageAssetId == asset.Id) &&
                !context.Users.AsNoTracking().Any(u => u.ProfileImageAssetId == asset.Id))
            .OrderBy(asset => asset.CreatedOnUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }
}
