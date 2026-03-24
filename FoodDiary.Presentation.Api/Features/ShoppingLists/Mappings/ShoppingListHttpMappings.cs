using FoodDiary.Application.ShoppingLists.Commands.Common;
using FoodDiary.Application.ShoppingLists.Commands.CreateShoppingList;
using FoodDiary.Application.ShoppingLists.Commands.DeleteShoppingList;
using FoodDiary.Application.ShoppingLists.Commands.UpdateShoppingList;
using FoodDiary.Application.ShoppingLists.Queries.GetCurrentShoppingList;
using FoodDiary.Application.ShoppingLists.Queries.GetShoppingListById;
using FoodDiary.Application.ShoppingLists.Queries.GetShoppingLists;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Presentation.Api.Features.ShoppingLists.Requests;

namespace FoodDiary.Presentation.Api.Features.ShoppingLists.Mappings;

public static class ShoppingListHttpMappings {
    public static GetCurrentShoppingListQuery ToCurrentQuery(this Guid userId) => new(userId);

    public static GetShoppingListsQuery ToListQuery(this Guid userId) => new(userId);

    public static GetShoppingListByIdQuery ToGetByIdQuery(this Guid shoppingListId, Guid userId) =>
        new(userId, new ShoppingListId(shoppingListId));

    public static DeleteShoppingListCommand ToDeleteCommand(this Guid shoppingListId, Guid userId) =>
        new(userId, new ShoppingListId(shoppingListId));

    public static CreateShoppingListCommand ToCommand(this CreateShoppingListHttpRequest request, Guid userId) =>
        new(
            userId,
            request.Name,
            request.Items?.Select(ToInput).ToList() ?? new());

    public static UpdateShoppingListCommand ToCommand(
        this UpdateShoppingListHttpRequest request,
        Guid userId,
        Guid shoppingListId) =>
        new(
            userId,
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
