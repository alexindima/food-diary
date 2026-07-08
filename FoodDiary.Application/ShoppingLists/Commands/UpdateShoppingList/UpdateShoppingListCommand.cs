using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.ShoppingLists.Common;
using FoodDiary.Application.ShoppingLists.Models;

namespace FoodDiary.Application.ShoppingLists.Commands.UpdateShoppingList;

public record UpdateShoppingListCommand(
    Guid? UserId,
    Guid ShoppingListId,
    string? Name,
    IReadOnlyList<ShoppingListItemInput>? Items) : ICommand<Result<ShoppingListModel>>, IUserRequest;
