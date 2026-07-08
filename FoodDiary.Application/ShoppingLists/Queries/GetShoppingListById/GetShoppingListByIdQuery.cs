using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.ShoppingLists.Models;

namespace FoodDiary.Application.ShoppingLists.Queries.GetShoppingListById;

public record GetShoppingListByIdQuery(
    Guid? UserId,
    Guid ShoppingListId) : IQuery<Result<ShoppingListModel>>, IUserRequest;
