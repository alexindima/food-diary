using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.ShoppingLists.Common;
using FoodDiary.Application.ShoppingLists.Mappings;
using FoodDiary.Application.ShoppingLists.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.ShoppingLists.Queries.GetShoppingLists;

public class GetShoppingListsQueryHandler(IShoppingListRepository shoppingListRepository)
    : IQueryHandler<GetShoppingListsQuery, Result<IReadOnlyList<ShoppingListSummaryModel>>> {
    public async Task<Result<IReadOnlyList<ShoppingListSummaryModel>>> Handle(
        GetShoppingListsQuery query,
        CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<IReadOnlyList<ShoppingListSummaryModel>>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(query.UserId.Value);

        var lists = await shoppingListRepository.GetAllAsync(
            userId,
            includeItems: true,
            cancellationToken: cancellationToken);

        var response = lists
            .Select(list => list.ToSummaryModel())
            .ToList();

        return Result.Success<IReadOnlyList<ShoppingListSummaryModel>>(response);
    }
}
