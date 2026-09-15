using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Models;

namespace FoodDiary.Modules.MealPlanning.Application.MealPlans.Commands.GenerateShoppingList;

public record GenerateShoppingListCommand(
    Guid? UserId,
    Guid PlanId) : ICommand<Result<ShoppingListModel>>, IUserRequest;
