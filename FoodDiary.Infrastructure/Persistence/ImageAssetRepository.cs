using FoodDiary.Application.Images.Common;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence;

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
        var productUse = await context.Products.AnyAsync(p => p.ImageAssetId == assetId, cancellationToken);
        if (productUse) return true;

        var recipeUse = await context.Recipes.AnyAsync(r => r.ImageAssetId == assetId, cancellationToken);
        if (recipeUse) return true;

        var stepUse = await context.RecipeSteps.AnyAsync(s => s.ImageAssetId == assetId, cancellationToken);
        if (stepUse) return true;

        var mealUse = await context.Meals.AnyAsync(m => m.ImageAssetId == assetId, cancellationToken);
        if (mealUse) return true;

        var userUse = await context.Users.AnyAsync(u => u.ProfileImageAssetId == assetId, cancellationToken);
        return userUse;
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
                !context.Users.Any(u => u.ProfileImageAssetId == asset.Id))
            .OrderBy(asset => asset.CreatedOnUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }
}
