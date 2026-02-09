using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Contracts.ShoppingLists;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.ShoppingLists.Queries.GetCurrentShoppingList;

public record GetCurrentShoppingListQuery(
    UserId? UserId) : IQuery<Result<ShoppingListResponse>>;
