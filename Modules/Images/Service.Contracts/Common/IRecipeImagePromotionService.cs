using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Images.Service.Contracts.Common;

public interface IRecipeImagePromotionService {
    Task<Result> PromoteAsync(ImageAssetId assetId, UserId ownerId, CancellationToken cancellationToken);
}
