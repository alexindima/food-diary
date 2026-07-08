using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.ShoppingLists.Commands.DeleteShoppingList;

public record DeleteShoppingListCommand(
    Guid? UserId,
    Guid ShoppingListId) : ICommand<Result>, IUserRequest;
