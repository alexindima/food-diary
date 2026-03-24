using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;

namespace FoodDiary.Application.ShoppingLists.Commands.DeleteShoppingList;

public record DeleteShoppingListCommand(
    Guid? UserId,
    Guid ShoppingListId) : ICommand<Result<bool>>;
