using FoodDiary.Domain.Entities.Ai;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.Entities.Content;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Entities.Shopping;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Common.Interfaces.Persistence;

public interface IImageAssetRepository
{
    Task<ImageAsset> AddAsync(ImageAsset asset, CancellationToken cancellationToken = default);
    Task<ImageAsset?> GetByIdAsync(ImageAssetId id, CancellationToken cancellationToken = default);
    Task DeleteAsync(ImageAsset asset, CancellationToken cancellationToken = default);
    Task<bool> IsAssetInUse(ImageAssetId assetId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ImageAsset>> GetUnusedOlderThanAsync(
        DateTime olderThanUtc,
        int batchSize,
        CancellationToken cancellationToken = default);
}


