using FoodDiary.Application.ShoppingLists.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.ShoppingLists.Common;

public interface IShoppingListCreationService {
    Task<Result<ShoppingListModel>> CreateAsync(
        ShoppingListCreationRequest request,
        CancellationToken cancellationToken);
}
