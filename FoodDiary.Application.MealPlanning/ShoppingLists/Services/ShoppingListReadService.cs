using FoodDiary.Application.Abstractions.ShoppingLists.Common;
using FoodDiary.Application.Abstractions.ShoppingLists.Models;
using FoodDiary.Application.MealPlanning.ShoppingLists.Common;
using FoodDiary.Application.MealPlanning.ShoppingLists.Mappings;
using FoodDiary.Application.MealPlanning.ShoppingLists.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.MealPlanning.ShoppingLists.Services;

public sealed class ShoppingListReadService(IShoppingListReadModelRepository shoppingListRepository)
    : IShoppingListReadService {
    public async Task<IReadOnlyList<ShoppingListSummaryModel>> GetAllAsync(
        UserId userId,
        CancellationToken cancellationToken) {
        IReadOnlyList<ShoppingListSummaryReadModel> lists = await shoppingListRepository.GetAllSummaryReadModelsAsync(
            userId,
            cancellationToken).ConfigureAwait(false);

        return lists
            .Select(list => list.ToSummaryModel())
            .ToList();
    }

    public async Task<ShoppingListModel?> GetByIdAsync(
        ShoppingListId shoppingListId,
        UserId userId,
        CancellationToken cancellationToken) {
        ShoppingListReadModel? list = await shoppingListRepository.GetReadModelByIdAsync(
            shoppingListId,
            userId,
            cancellationToken).ConfigureAwait(false);

        return list?.ToModel();
    }

    public async Task<ShoppingListModel?> GetCurrentAsync(
        UserId userId,
        CancellationToken cancellationToken) {
        ShoppingListReadModel? list = await shoppingListRepository.GetCurrentReadModelAsync(
            userId,
            cancellationToken).ConfigureAwait(false);

        return list?.ToModel();
    }
}
