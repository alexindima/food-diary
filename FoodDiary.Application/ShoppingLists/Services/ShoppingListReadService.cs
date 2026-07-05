using FoodDiary.Application.Abstractions.ShoppingLists.Common;
using FoodDiary.Application.ShoppingLists.Common;
using FoodDiary.Application.ShoppingLists.Mappings;
using FoodDiary.Application.ShoppingLists.Models;
using FoodDiary.Domain.Entities.Shopping;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.ShoppingLists.Services;

public sealed class ShoppingListReadService(IShoppingListReadRepository shoppingListRepository)
    : IShoppingListReadService {
    public async Task<IReadOnlyList<ShoppingListSummaryModel>> GetAllAsync(
        UserId userId,
        CancellationToken cancellationToken) {
        IReadOnlyList<ShoppingList> lists = await shoppingListRepository.GetAllAsync(
            userId,
            includeItems: true,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return lists
            .Select(list => list.ToSummaryModel())
            .ToList();
    }

    public async Task<ShoppingListModel?> GetByIdAsync(
        ShoppingListId shoppingListId,
        UserId userId,
        CancellationToken cancellationToken) {
        ShoppingList? list = await shoppingListRepository.GetByIdAsync(
            shoppingListId,
            userId,
            includeItems: true,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return list?.ToModel();
    }

    public async Task<ShoppingListModel?> GetCurrentAsync(
        UserId userId,
        CancellationToken cancellationToken) {
        ShoppingList? list = await shoppingListRepository.GetCurrentAsync(
            userId,
            includeItems: true,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return list?.ToModel();
    }
}
