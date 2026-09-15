using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Common;
using FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Models;

namespace FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Commands.UpdateShoppingList;

public record UpdateShoppingListCommand(
    Guid? UserId,
    Guid ShoppingListId,
    string? Name,
    IReadOnlyList<ShoppingListItemInput>? Items) : ICommand<Result<ShoppingListModel>>, IUserRequest;
