using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.ShoppingLists.Models;

namespace FoodDiary.Application.ShoppingLists.Queries.GetCurrentShoppingList;

public record GetCurrentShoppingListQuery(
    Guid? UserId) : IQuery<Result<ShoppingListModel>>, IUserRequest;
