using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.ShoppingLists.Models;

namespace FoodDiary.Application.ShoppingLists.Queries.GetShoppingLists;

public record GetShoppingListsQuery(
    Guid? UserId) : IQuery<Result<IReadOnlyList<ShoppingListSummaryModel>>>, IUserRequest;
