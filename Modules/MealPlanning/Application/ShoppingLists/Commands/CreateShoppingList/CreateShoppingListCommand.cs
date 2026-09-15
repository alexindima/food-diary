using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Common;
using FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Models;

namespace FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Commands.CreateShoppingList;

public record CreateShoppingListCommand(
    Guid? UserId,
    string Name,
    IReadOnlyList<ShoppingListItemInput> Items) : ICommand<Result<ShoppingListModel>>, IUserRequest;
