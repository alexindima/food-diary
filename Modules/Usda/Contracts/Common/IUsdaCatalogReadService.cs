using FoodDiary.Modules.Usda.Contracts.Models;

namespace FoodDiary.Modules.Usda.Contracts.Common;

public interface IUsdaCatalogReadService {
    Task<IReadOnlyDictionary<int, UsdaCatalogFoodModel>> ReadLocalFoodsAsync(
        IReadOnlyList<int> fdcIds, CancellationToken cancellationToken);
}
