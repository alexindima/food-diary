using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Abstractions.Images.Common;

public interface IImageAssetContentService {
    // The owner validates access and confirmation. Content is transient and must not be logged or persisted.
    Task<Result<string>> GetDataUrlAsync(ImageAssetId assetId, UserId userId, CancellationToken cancellationToken);
}
