using FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Common;

public interface IShoppingListCreationService {
    Task<Result<ShoppingListModel>> CreateAsync(
        ShoppingListCreationRequest request,
        CancellationToken cancellationToken);
}
