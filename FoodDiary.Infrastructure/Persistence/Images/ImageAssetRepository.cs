using FoodDiary.Application.Images.Common;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Images;

public class ImageAssetRepository(FoodDiaryDbContext context) : IImageAssetRepository {
    public async Task<ImageAsset> AddAsync(ImageAsset asset, CancellationToken cancellationToken = default) {
        context.ImageAssets.Add(asset);
        await context.SaveChangesAsync(cancellationToken);
        return asset;
    }

    public async Task<ImageAsset?> GetByIdAsync(ImageAssetId id, CancellationToken cancellationToken = default) {
        return await context.ImageAssets.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task DeleteAsync(ImageAsset asset, CancellationToken cancellationToken = default) {
        context.ImageAssets.Attach(asset);
        context.ImageAssets.Remove(asset);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> IsAssetInUse(ImageAssetId assetId, CancellationToken cancellationToken = default) {
        return await context.ImageAssets
            .Where(a => a.Id == assetId)
            .Select(_ =>
                context.Products.Any(p => p.ImageAssetId == assetId) ||
                context.Recipes.Any(r => r.ImageAssetId == assetId) ||
                context.RecipeSteps.Any(s => s.ImageAssetId == assetId) ||
                context.Meals.Any(m => m.ImageAssetId == assetId) ||
                context.MealAiSessions.Any(s => s.ImageAssetId == assetId) ||
                context.Users.Any(u => u.ProfileImageAssetId == assetId))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ImageAsset>> GetUnusedOlderThanAsync(
        DateTime olderThanUtc,
        int batchSize,
        CancellationToken cancellationToken = default) {
        return await context.ImageAssets
            .AsNoTracking()
            .Where(asset =>
                asset.CreatedOnUtc < olderThanUtc &&
                !context.Products.Any(p => p.ImageAssetId == asset.Id) &&
                !context.Recipes.Any(r => r.ImageAssetId == asset.Id) &&
                !context.RecipeSteps.Any(s => s.ImageAssetId == asset.Id) &&
                !context.Meals.Any(m => m.ImageAssetId == asset.Id) &&
                !context.MealAiSessions.Any(s => s.ImageAssetId == asset.Id) &&
                !context.Users.Any(u => u.ProfileImageAssetId == asset.Id))
            .OrderBy(asset => asset.CreatedOnUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }
}
