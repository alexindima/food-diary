using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.ShoppingLists.Common;
using FoodDiary.Application.ShoppingLists.Mappings;
using FoodDiary.Application.ShoppingLists.Models;

namespace FoodDiary.Application.ShoppingLists.Queries.GetShoppingLists;

public class GetShoppingListsQueryHandler(IShoppingListRepository shoppingListRepository)
    : IQueryHandler<GetShoppingListsQuery, Result<IReadOnlyList<ShoppingListSummaryModel>>> {
    public async Task<Result<IReadOnlyList<ShoppingListSummaryModel>>> Handle(
        GetShoppingListsQuery query,
        CancellationToken cancellationToken) {
        var userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<IReadOnlyList<ShoppingListSummaryModel>>(userIdResult.Error);
        }

        var userId = userIdResult.Value;

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
