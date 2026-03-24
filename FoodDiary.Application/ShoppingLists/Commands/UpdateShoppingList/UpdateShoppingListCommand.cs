using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.ShoppingLists.Commands.Common;
using FoodDiary.Application.ShoppingLists.Models;

namespace FoodDiary.Application.ShoppingLists.Commands.UpdateShoppingList;

public record UpdateShoppingListCommand(
    Guid? UserId,
    Guid ShoppingListId,
    string? Name,
    IReadOnlyList<ShoppingListItemInput>? Items) : ICommand<Result<ShoppingListModel>>;
