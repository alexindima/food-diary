using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Contracts.ShoppingLists;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.ShoppingLists.Queries.GetShoppingLists;

public record GetShoppingListsQuery(
    UserId? UserId) : IQuery<Result<IReadOnlyList<ShoppingListSummaryResponse>>>;
