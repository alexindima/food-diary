using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.ShoppingLists.Mappings;
using FoodDiary.Application.ShoppingLists.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.ShoppingLists.Queries.GetShoppingLists;

public class GetShoppingListsQueryHandler(IShoppingListRepository shoppingListRepository)
    : IQueryHandler<GetShoppingListsQuery, Result<IReadOnlyList<ShoppingListSummaryModel>>> {
    public async Task<Result<IReadOnlyList<ShoppingListSummaryModel>>> Handle(
        GetShoppingListsQuery query,
        CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == UserId.Empty) {
            return Result.Failure<IReadOnlyList<ShoppingListSummaryModel>>(Errors.Authentication.InvalidToken);
        }

        var lists = await shoppingListRepository.GetAllAsync(
            query.UserId.Value,
            includeItems: true,
            cancellationToken: cancellationToken);

        var response = lists
            .Select(list => list.ToSummaryModel())
            .ToList();

        return Result.Success<IReadOnlyList<ShoppingListSummaryModel>>(response);
    }
}
