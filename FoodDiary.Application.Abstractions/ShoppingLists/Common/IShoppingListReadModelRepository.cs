using FoodDiary.Application.Abstractions.ShoppingLists.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.ShoppingLists.Common;

public interface IShoppingListReadModelRepository {
    Task<ShoppingListReadModel?> GetReadModelByIdAsync(
        ShoppingListId id,
        UserId userId,
        CancellationToken cancellationToken = default);

    Task<ShoppingListReadModel?> GetCurrentReadModelAsync(
        UserId userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ShoppingListSummaryReadModel>> GetAllSummaryReadModelsAsync(
        UserId userId,
        CancellationToken cancellationToken = default);
}
