using FoodDiary.Application.Abstractions.ShoppingLists.Common;
using FoodDiary.Application.ShoppingLists.Common;
using FoodDiary.Application.ShoppingLists.Mappings;
using FoodDiary.Application.ShoppingLists.Models;
using FoodDiary.Domain.Entities.Shopping;
using FoodDiary.Results;

namespace FoodDiary.Application.ShoppingLists.Services;

internal sealed class ShoppingListCreationService(IShoppingListWriteRepository shoppingListRepository)
    : IShoppingListCreationService {
    public async Task<Result<ShoppingListModel>> CreateAsync(
        ShoppingListCreationRequest request,
        CancellationToken cancellationToken) {
        var shoppingList = ShoppingList.Create(request.UserId, request.Name);
        foreach (ShoppingListCreationItem item in request.Items.OrderBy(static item => item.SortOrder)) {
            ShoppingListItem shoppingListItem = shoppingList.AddItem(
                item.Name,
                item.ProductId,
                item.Amount,
                item.Unit,
                item.Category,
                isChecked: false,
                item.SortOrder);

            foreach (ShoppingListCreationSource source in item.Sources) {
                shoppingListItem.AddMealPlanSource(
                    source.MealPlanId,
                    source.MealPlanMealId,
                    source.RecipeId,
                    source.Label,
                    source.DayNumber,
                    source.MealType,
                    source.Amount,
                    source.Unit);
            }
        }

        await shoppingListRepository.AddAsync(shoppingList, cancellationToken).ConfigureAwait(false);
        return Result.Success(shoppingList.ToModel());
    }
}
