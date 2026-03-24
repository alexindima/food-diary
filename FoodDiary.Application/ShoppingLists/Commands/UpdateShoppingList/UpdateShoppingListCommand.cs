using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.ShoppingLists.Commands.Common;
using FoodDiary.Application.ShoppingLists.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.ShoppingLists.Commands.UpdateShoppingList;

public record UpdateShoppingListCommand(
    Guid? UserId,
    ShoppingListId ShoppingListId,
    string? Name,
    IReadOnlyList<ShoppingListItemInput>? Items) : ICommand<Result<ShoppingListModel>>;
