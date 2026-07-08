using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.ShoppingLists.Common;
using FoodDiary.Application.ShoppingLists.Models;

namespace FoodDiary.Application.ShoppingLists.Commands.CreateShoppingList;

public record CreateShoppingListCommand(
    Guid? UserId,
    string Name,
    IReadOnlyList<ShoppingListItemInput> Items) : ICommand<Result<ShoppingListModel>>, IUserRequest;
