using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.MealPlanning.ShoppingLists.Models;

namespace FoodDiary.Application.MealPlanning.ShoppingLists.Queries.GetCurrentShoppingList;

public record GetCurrentShoppingListQuery(
    Guid? UserId) : IQuery<Result<ShoppingListModel>>, IUserRequest;
