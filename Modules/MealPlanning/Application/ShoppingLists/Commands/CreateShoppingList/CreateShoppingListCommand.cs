using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.MealPlanning.ShoppingLists.Common;
using FoodDiary.Application.MealPlanning.ShoppingLists.Models;

namespace FoodDiary.Application.MealPlanning.ShoppingLists.Commands.CreateShoppingList;

public record CreateShoppingListCommand(
    Guid? UserId,
    string Name,
    IReadOnlyList<ShoppingListItemInput> Items) : ICommand<Result<ShoppingListModel>>, IUserRequest;
