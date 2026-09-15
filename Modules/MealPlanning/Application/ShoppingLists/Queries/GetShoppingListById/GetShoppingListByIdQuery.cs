using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Models;

namespace FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Queries.GetShoppingListById;

public record GetShoppingListByIdQuery(
    Guid? UserId,
    Guid ShoppingListId) : IQuery<Result<ShoppingListModel>>, IUserRequest;
