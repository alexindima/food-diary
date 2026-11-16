using FoodDiary.Modules.Usda.Application.Abstractions.Common;
using FoodDiary.Modules.Usda.Contracts.Common;
using FoodDiary.Modules.Usda.Contracts.Models;

namespace FoodDiary.Modules.Usda.Application.CatalogImport;

internal sealed class UsdaCatalogReadService(IUsdaFoodReadModelRepository repository) : IUsdaCatalogReadService {
    public async Task<IReadOnlyDictionary<int, UsdaCatalogFoodModel>> ReadLocalFoodsAsync(
        IReadOnlyList<int> fdcIds, CancellationToken cancellationToken) {
        IReadOnlyDictionary<int, IReadOnlyList<UsdaNutrientReadModel>> nutrients =
            await repository.GetNutrientReadModelsByFdcIdsAsync(fdcIds, cancellationToken);
        var foods = new Dictionary<int, UsdaCatalogFoodModel>(fdcIds.Count);
        foreach (int id in fdcIds.Distinct()) {
            UsdaFoodReadModel? food = await repository.GetByFdcIdReadModelAsync(id, cancellationToken);
            if (food is not null) {
                foods.Add(id, new UsdaCatalogFoodModel(food.Description, nutrients.GetValueOrDefault(id) ?? []));
            }
        }
        return foods;
    }
}
