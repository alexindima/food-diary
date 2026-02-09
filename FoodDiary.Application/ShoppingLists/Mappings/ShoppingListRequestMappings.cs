using System.Linq;
using FoodDiary.Application.ShoppingLists.Commands.Common;
using FoodDiary.Application.ShoppingLists.Commands.CreateShoppingList;
using FoodDiary.Application.ShoppingLists.Commands.UpdateShoppingList;
using FoodDiary.Contracts.ShoppingLists;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.ShoppingLists.Mappings;

public static class ShoppingListRequestMappings
{
    public static CreateShoppingListCommand ToCommand(this CreateShoppingListRequest request, Guid? userId) =>
        new(
            userId.HasValue ? new UserId(userId.Value) : null,
            request.Name,
            request.Items?.Select(ToInput).ToList() ?? new());

    public static UpdateShoppingListCommand ToCommand(
        this UpdateShoppingListRequest request,
        Guid? userId,
        Guid shoppingListId) =>
        new(
            userId.HasValue ? new UserId(userId.Value) : null,
            new ShoppingListId(shoppingListId),
            request.Name,
            request.Items?.Select(ToInput).ToList());

    private static ShoppingListItemInput ToInput(ShoppingListItemRequest request) =>
        new(
            request.ProductId,
            request.Name,
            request.Amount,
            request.Unit,
            request.Category,
            request.IsChecked,
            request.SortOrder);
}
