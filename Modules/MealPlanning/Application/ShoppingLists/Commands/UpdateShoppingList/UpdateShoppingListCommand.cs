using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.MealPlanning.ShoppingLists.Common;
using FoodDiary.Application.MealPlanning.ShoppingLists.Models;

namespace FoodDiary.Application.MealPlanning.ShoppingLists.Commands.UpdateShoppingList;

public record UpdateShoppingListCommand(
    Guid? UserId,
    Guid ShoppingListId,
    string? Name,
    IReadOnlyList<ShoppingListItemInput>? Items) : ICommand<Result<ShoppingListModel>>, IUserRequest;
