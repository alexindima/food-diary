using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.ShoppingLists.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.ShoppingLists.Queries.GetShoppingListById;

public record GetShoppingListByIdQuery(
    UserId? UserId,
    ShoppingListId ShoppingListId) : IQuery<Result<ShoppingListModel>>;
