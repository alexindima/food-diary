using FoodDiary.Application.MealPlanning.ShoppingLists.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.MealPlanning.ShoppingLists.Common;

public interface IShoppingListCreationService {
    Task<Result<ShoppingListModel>> CreateAsync(
        ShoppingListCreationRequest request,
        CancellationToken cancellationToken);
}
