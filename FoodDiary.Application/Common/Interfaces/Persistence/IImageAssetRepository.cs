using FoodDiary.Domain.Entities;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Common.Interfaces.Persistence;

public interface IImageAssetRepository
{
    Task<ImageAsset> AddAsync(ImageAsset asset, CancellationToken cancellationToken = default);
    Task<ImageAsset?> GetByIdAsync(ImageAssetId id, CancellationToken cancellationToken = default);
    Task DeleteAsync(ImageAsset asset, CancellationToken cancellationToken = default);
    Task<bool> IsAssetInUse(ImageAssetId assetId, CancellationToken cancellationToken = default);
}
