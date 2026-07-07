using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.ShoppingLists.Common;
using FoodDiary.Application.ShoppingLists.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.ShoppingLists.Queries.GetShoppingListById;

public sealed class GetShoppingListByIdQueryHandler(
    IShoppingListReadService shoppingListReadService,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetShoppingListByIdQuery, Result<ShoppingListModel>> {
    public async Task<Result<ShoppingListModel>> Handle(
        GetShoppingListByIdQuery query,
        CancellationToken cancellationToken) {
        if (query.ShoppingListId == Guid.Empty) {
            return Result.Failure<ShoppingListModel>(
                Errors.Validation.Invalid(nameof(query.ShoppingListId), "Shopping list id must not be empty."));
        }

        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<ShoppingListModel>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        var shoppingListId = new ShoppingListId(query.ShoppingListId);

        ShoppingListModel? list = await shoppingListReadService.GetByIdAsync(
            shoppingListId,
            userId,
            cancellationToken).ConfigureAwait(false);

        return list is null
            ? Result.Failure<ShoppingListModel>(Errors.ShoppingList.NotFound(query.ShoppingListId))
            : Result.Success(list);
    }
}
