using FoodDiary.Domain.Entities.Shopping;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.ShoppingLists.Common;

public interface IShoppingListRepository {
    Task<ShoppingList> AddAsync(ShoppingList list, CancellationToken cancellationToken = default);

    Task<ShoppingList?> GetByIdAsync(
        ShoppingListId id,
        UserId userId,
        bool includeItems = false,
        bool asTracking = false,
        CancellationToken cancellationToken = default);

    Task<ShoppingList?> GetCurrentAsync(
        UserId userId,
        bool includeItems = false,
        bool asTracking = false,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ShoppingList>> GetAllAsync(
        UserId userId,
        bool includeItems = false,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(ShoppingList list, CancellationToken cancellationToken = default);

    Task DeleteAsync(ShoppingList list, CancellationToken cancellationToken = default);
}
