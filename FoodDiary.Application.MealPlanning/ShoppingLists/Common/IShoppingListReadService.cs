using FoodDiary.Application.ShoppingLists.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.ShoppingLists.Common;

public interface IShoppingListReadService {
    Task<IReadOnlyList<ShoppingListSummaryModel>> GetAllAsync(
        UserId userId,
        CancellationToken cancellationToken);

    Task<ShoppingListModel?> GetByIdAsync(
        ShoppingListId shoppingListId,
        UserId userId,
        CancellationToken cancellationToken);

    Task<ShoppingListModel?> GetCurrentAsync(
        UserId userId,
        CancellationToken cancellationToken);
}
