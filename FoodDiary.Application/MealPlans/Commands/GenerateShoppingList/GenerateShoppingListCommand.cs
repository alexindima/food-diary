using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.ShoppingLists.Models;

namespace FoodDiary.Application.MealPlans.Commands.GenerateShoppingList;

public record GenerateShoppingListCommand(
    Guid? UserId,
    Guid PlanId) : ICommand<Result<ShoppingListModel>>, IUserRequest;
