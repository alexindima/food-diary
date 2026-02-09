using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Contracts.ShoppingLists;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.ShoppingLists.Queries.GetShoppingListById;

public record GetShoppingListByIdQuery(
    UserId? UserId,
    ShoppingListId ShoppingListId) : IQuery<Result<ShoppingListResponse>>;
