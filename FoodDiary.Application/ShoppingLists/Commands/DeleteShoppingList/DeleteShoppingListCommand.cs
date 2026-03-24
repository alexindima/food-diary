using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.ShoppingLists.Commands.DeleteShoppingList;

public record DeleteShoppingListCommand(
    Guid? UserId,
    ShoppingListId ShoppingListId) : ICommand<Result<bool>>;
