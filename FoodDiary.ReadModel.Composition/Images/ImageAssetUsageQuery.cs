using FoodDiary.Modules.Images.Application.Abstractions.Models;
using FoodDiary.Modules.Images.Domain.Entities.Assets;
using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Images.Application.Abstractions.Common;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.ReadModel.Composition.Images;

public sealed class ImageAssetUsageQuery(ICompositionReadContext context) : IImageAssetUsageQuery {
    public async Task<bool> IsAssetInUseAsync(ImageAssetId assetId, CancellationToken cancellationToken = default) {
        return await context.ImageAssets.AsNoTracking()
            .Where(a => a.Id == assetId)
            .Select(_ =>
                context.Products.AsNoTracking().Any(p => p.ImageAssetId == assetId) ||
                context.Recipes.AsNoTracking().Any(r => r.ImageAssetId == assetId) ||
                context.RecipeSteps.AsNoTracking().Any(s => s.ImageAssetId == assetId) ||
                context.Meals.AsNoTracking().Any(m => m.ImageAssetId == assetId) ||
                context.MealAiSessions.AsNoTracking().Any(s => s.ImageAssetId == assetId) ||
                context.FoodRecognitionImageAssets.AsNoTracking().Any(asset => asset.Id == assetId) ||
                context.Users.AsNoTracking().Any(u => u.ProfileImageAssetId == assetId))
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ImageCleanupCandidate>> GetUnusedCandidatesOlderThanAsync(
        DateTime olderThanUtc,
        int batchSize,
        ImageCleanupCandidate? after = null,
        CancellationToken cancellationToken = default) {
        IQueryable<ImageAsset> candidates = context.ImageAssets
            .AsNoTracking()
            .Where(asset =>
                asset.CreatedOnUtc < olderThanUtc &&
                !context.Products.AsNoTracking().Any(p => p.ImageAssetId == asset.Id) &&
                !context.Recipes.AsNoTracking().Any(r => r.ImageAssetId == asset.Id) &&
                !context.RecipeSteps.AsNoTracking().Any(s => s.ImageAssetId == asset.Id) &&
                !context.Meals.AsNoTracking().Any(m => m.ImageAssetId == asset.Id) &&
                !context.MealAiSessions.AsNoTracking().Any(s => s.ImageAssetId == asset.Id) &&
                !context.FoodRecognitionImageAssets.AsNoTracking().Any(image => image.Id == asset.Id) &&
                !context.Users.AsNoTracking().Any(u => u.ProfileImageAssetId == asset.Id));
        if (after is not null) {
            candidates = candidates.Where(asset => EF.Functions.GreaterThan(
                ValueTuple.Create(asset.CreatedOnUtc, asset.Id), ValueTuple.Create(after.CreatedOnUtc, after.Id)));
        }
        return await candidates
            .OrderBy(asset => asset.CreatedOnUtc)
            .ThenBy(asset => asset.Id)
            .Take(batchSize)
            .Select(asset => new ImageCleanupCandidate(asset.Id, asset.CreatedOnUtc))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }
}
