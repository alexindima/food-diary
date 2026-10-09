using FoodDiary.Modules.MealPlanning.Domain.ValueObjects;
using FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Mappings;
using FoodDiary.Modules.MealPlanning.Application.Abstractions.ShoppingLists.Common;
using FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Common;

using FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Models;
using FoodDiary.Modules.MealPlanning.Domain.Entities.Shopping;
using FoodDiary.Results;

namespace FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Services;

internal sealed class ShoppingListCreationService(IShoppingListWriteRepository shoppingListRepository)
    : IShoppingListCreationService {
    public async Task<Result<ShoppingListModel>> CreateAsync(
        ShoppingListCreationRequest request,
        CancellationToken cancellationToken) {
        var shoppingList = ShoppingList.Create(request.UserId, request.Name);
        foreach (ShoppingListCreationItem item in request.Items.OrderBy(static item => item.SortOrder)) {
            ShoppingListItem shoppingListItem = shoppingList.AddItemWithQuantity(
                item.Name,
                item.ProductId,
                ShoppingQuantity.FromFields(item.Amount, item.Unit),
                item.Category,
                isChecked: false,
                item.SortOrder);

            foreach (ShoppingListCreationSource source in item.Sources) {
                shoppingListItem.AddMealPlanSourceWithQuantity(
                    source.MealPlanId,
                    source.MealPlanMealId,
                    source.RecipeId,
                    source.Label,
                    source.DayNumber,
                    source.MealType,
                    ShoppingSourceQuantity.FromFields(source.Amount, source.Unit));
            }
        }

        await shoppingListRepository.AddAsync(shoppingList, cancellationToken).ConfigureAwait(false);
        return Result.Success(shoppingList.ToModel());
    }
}
