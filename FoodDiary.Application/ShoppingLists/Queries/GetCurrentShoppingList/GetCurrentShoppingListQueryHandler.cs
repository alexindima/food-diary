using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.ShoppingLists.Common;
using FoodDiary.Application.ShoppingLists.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.ShoppingLists.Queries.GetCurrentShoppingList;

public sealed class GetCurrentShoppingListQueryHandler(
    IShoppingListReadService shoppingListReadService,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetCurrentShoppingListQuery, Result<ShoppingListModel>> {
    public async Task<Result<ShoppingListModel>> Handle(
        GetCurrentShoppingListQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<ShoppingListModel>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        ShoppingListModel? list = await shoppingListReadService.GetCurrentAsync(userId, cancellationToken).ConfigureAwait(false);

        return list is null
            ? Result.Failure<ShoppingListModel>(Errors.ShoppingList.CurrentNotFound())
            : Result.Success(list);
    }
}
