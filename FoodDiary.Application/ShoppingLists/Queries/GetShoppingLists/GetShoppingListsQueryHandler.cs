using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.ShoppingLists.Common;
using FoodDiary.Application.ShoppingLists.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.ShoppingLists.Queries.GetShoppingLists;

public sealed class GetShoppingListsQueryHandler(
    IShoppingListReadService shoppingListReadService,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetShoppingListsQuery, Result<IReadOnlyList<ShoppingListSummaryModel>>> {
    public async Task<Result<IReadOnlyList<ShoppingListSummaryModel>>> Handle(
        GetShoppingListsQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<IReadOnlyList<ShoppingListSummaryModel>>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        IReadOnlyList<ShoppingListSummaryModel> response = await shoppingListReadService
            .GetAllAsync(userId, cancellationToken)
            .ConfigureAwait(false);

        return Result.Success(response);
    }
}
