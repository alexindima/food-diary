using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.ShoppingLists.Mappings;
using FoodDiary.Contracts.ShoppingLists;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.ShoppingLists.Queries.GetShoppingLists;

public class GetShoppingListsQueryHandler(IShoppingListRepository shoppingListRepository)
    : IQueryHandler<GetShoppingListsQuery, Result<IReadOnlyList<ShoppingListSummaryResponse>>> {
    public async Task<Result<IReadOnlyList<ShoppingListSummaryResponse>>> Handle(
        GetShoppingListsQuery query,
        CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == UserId.Empty) {
            return Result.Failure<IReadOnlyList<ShoppingListSummaryResponse>>(Errors.Authentication.InvalidToken);
        }

        var lists = await shoppingListRepository.GetAllAsync(
            query.UserId.Value,
            includeItems: true,
            cancellationToken: cancellationToken);

        var response = lists
            .Select(list => list.ToSummaryResponse())
            .ToList();

        return Result.Success<IReadOnlyList<ShoppingListSummaryResponse>>(response);
    }
}
