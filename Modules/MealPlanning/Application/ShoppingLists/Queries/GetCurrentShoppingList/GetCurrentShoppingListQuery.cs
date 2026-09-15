using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Models;

namespace FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Queries.GetCurrentShoppingList;

public record GetCurrentShoppingListQuery(
    Guid? UserId) : IQuery<Result<ShoppingListModel>>, IUserRequest;
