using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.MealPlanning.ShoppingLists.Models;

namespace FoodDiary.Application.MealPlanning.ShoppingLists.Queries.GetShoppingListById;

public record GetShoppingListByIdQuery(
    Guid? UserId,
    Guid ShoppingListId) : IQuery<Result<ShoppingListModel>>, IUserRequest;
