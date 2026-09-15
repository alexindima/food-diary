using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Images.Service.Contracts.Common;

public interface IImageAssetContentService {
    // The owner validates access and confirmation. Content is transient and must not be logged or persisted.
    Task<Result<string>> GetDataUrlAsync(ImageAssetId assetId, UserId userId, CancellationToken cancellationToken);
}
