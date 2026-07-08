using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Common.Validation;
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
        Result<ShoppingListId> shoppingListIdResult = RequiredIdParser.Parse(
            query.ShoppingListId,
            nameof(query.ShoppingListId),
            "Shopping list id must not be empty.",
            value => new ShoppingListId(value));
        if (shoppingListIdResult.IsFailure) {
            return RequiredIdParser.ToFailure<ShoppingListModel, ShoppingListId>(shoppingListIdResult);
        }

        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<ShoppingListModel>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        ShoppingListId shoppingListId = shoppingListIdResult.Value;

        ShoppingListModel? list = await shoppingListReadService.GetByIdAsync(
            shoppingListId,
            userId,
            cancellationToken).ConfigureAwait(false);

        return list is null
            ? Result.Failure<ShoppingListModel>(Errors.ShoppingList.NotFound(query.ShoppingListId))
            : Result.Success(list);
    }
}
