using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.ShoppingLists.Commands.Common;
using FoodDiary.Application.ShoppingLists.Models;

namespace FoodDiary.Application.ShoppingLists.Commands.CreateShoppingList;

public record CreateShoppingListCommand(
    Guid? UserId,
    string Name,
    IReadOnlyList<ShoppingListItemInput> Items) : ICommand<Result<ShoppingListModel>>, IUserRequest;
