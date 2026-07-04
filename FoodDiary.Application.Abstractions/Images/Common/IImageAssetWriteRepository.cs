using FoodDiary.Domain.Entities.Assets;

namespace FoodDiary.Application.Abstractions.Images.Common;

public interface IImageAssetWriteRepository : IImageAssetReadRepository {
    Task<ImageAsset> AddAsync(ImageAsset asset, CancellationToken cancellationToken = default);

    Task DeleteAsync(ImageAsset asset, CancellationToken cancellationToken = default);
}
