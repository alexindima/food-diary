using FoodDiary.Application.ShoppingLists.Commands.Common;
using FoodDiary.Application.ShoppingLists.Commands.CreateShoppingList;
using FoodDiary.Application.ShoppingLists.Commands.UpdateShoppingList;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Presentation.Api.Features.ShoppingLists.Requests;

namespace FoodDiary.Presentation.Api.Features.ShoppingLists.Mappings;

public static class ShoppingListHttpMappings {
    public static CreateShoppingListCommand ToCommand(this CreateShoppingListHttpRequest request, Guid userId) =>
        new(
            new UserId(userId),
            request.Name,
            request.Items?.Select(ToInput).ToList() ?? new());

    public static UpdateShoppingListCommand ToCommand(
        this UpdateShoppingListHttpRequest request,
        Guid userId,
        Guid shoppingListId) =>
        new(
            new UserId(userId),
            new ShoppingListId(shoppingListId),
            request.Name,
            request.Items?.Select(ToInput).ToList());

    private static ShoppingListItemInput ToInput(ShoppingListItemHttpRequest request) =>
        new(
            request.ProductId,
            request.Name,
            request.Amount,
            request.Unit,
            request.Category,
            request.IsChecked,
            request.SortOrder);
}
