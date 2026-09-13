using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.ReadModel.Composition.Images;

public sealed class ImageAssetUsageQuery(FoodDiaryDbContext context) : IImageAssetUsageQuery {
    public async Task<bool> IsAssetInUseAsync(ImageAssetId assetId, CancellationToken cancellationToken = default) {
        return await context.ImageAssets.AsNoTracking()
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

    public async Task<IReadOnlyList<ImageAssetId>> GetUnusedIdsOlderThanAsync(
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
            .Select(asset => asset.Id)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }
}
