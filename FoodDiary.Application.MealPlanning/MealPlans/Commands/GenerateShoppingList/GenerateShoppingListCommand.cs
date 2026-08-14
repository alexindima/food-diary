using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.MealPlanning.ShoppingLists.Models;

namespace FoodDiary.Application.MealPlanning.MealPlans.Commands.GenerateShoppingList;

public record GenerateShoppingListCommand(
    Guid? UserId,
    Guid PlanId) : ICommand<Result<ShoppingListModel>>, IUserRequest;
