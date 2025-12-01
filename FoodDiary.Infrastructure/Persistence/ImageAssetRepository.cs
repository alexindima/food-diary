using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Domain.Entities;
using FoodDiary.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence;

public class ImageAssetRepository(FoodDiaryDbContext context) : IImageAssetRepository
{
    private readonly FoodDiaryDbContext _context = context;

    public async Task<ImageAsset> AddAsync(ImageAsset asset, CancellationToken cancellationToken = default)
    {
        _context.ImageAssets.Add(asset);
        await _context.SaveChangesAsync(cancellationToken);
        return asset;
    }

    public async Task<ImageAsset?> GetByIdAsync(ImageAssetId id, CancellationToken cancellationToken = default)
    {
        return await _context.ImageAssets.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task DeleteAsync(ImageAsset asset, CancellationToken cancellationToken = default)
    {
        _context.ImageAssets.Remove(asset);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> IsAssetInUse(ImageAssetId assetId, CancellationToken cancellationToken = default)
    {
        var productUse = await _context.Products.AnyAsync(p => p.ImageAssetId == assetId, cancellationToken);
        if (productUse) return true;

        var recipeUse = await _context.Recipes.AnyAsync(r => r.ImageAssetId == assetId, cancellationToken);
        if (recipeUse) return true;

        var mealUse = await _context.Meals.AnyAsync(m => m.ImageAssetId == assetId, cancellationToken);
        return mealUse;
    }
}
