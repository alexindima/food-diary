using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.ShoppingLists.Models;

namespace FoodDiary.Application.ShoppingLists.Queries.GetShoppingLists;

public record GetShoppingListsQuery(
    Guid? UserId) : IQuery<Result<IReadOnlyList<ShoppingListSummaryModel>>>, IUserRequest;
