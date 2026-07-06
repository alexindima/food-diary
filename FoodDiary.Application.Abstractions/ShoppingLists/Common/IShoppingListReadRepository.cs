using FoodDiary.Domain.Entities.Shopping;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.ShoppingLists.Common;

public interface IShoppingListReadRepository {
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
}