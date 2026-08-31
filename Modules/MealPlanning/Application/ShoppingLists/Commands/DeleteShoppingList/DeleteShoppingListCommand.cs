using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.MealPlanning.ShoppingLists.Commands.DeleteShoppingList;

public record DeleteShoppingListCommand(
    Guid? UserId,
    Guid ShoppingListId) : ICommand<Result>, IUserRequest;
