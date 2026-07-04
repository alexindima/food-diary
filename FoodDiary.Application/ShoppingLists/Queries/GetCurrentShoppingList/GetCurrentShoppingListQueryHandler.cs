using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.ShoppingLists.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.ShoppingLists.Mappings;
using FoodDiary.Application.ShoppingLists.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Shopping;

namespace FoodDiary.Application.ShoppingLists.Queries.GetCurrentShoppingList;

public class GetCurrentShoppingListQueryHandler(
    IShoppingListReadRepository shoppingListRepository,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetCurrentShoppingListQuery, Result<ShoppingListModel>> {
    public async Task<Result<ShoppingListModel>> Handle(
        GetCurrentShoppingListQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<ShoppingListModel>(userIdResult.Error);
        }

        UserId userId = userIdResult.Value;
        Error? accessError = await currentUserAccessService.EnsureCanAccessAsync(userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<ShoppingListModel>(accessError);
        }

        ShoppingList? list = await shoppingListRepository.GetCurrentAsync(
            userId,
            includeItems: true,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return list is null
            ? Result.Failure<ShoppingListModel>(Errors.ShoppingList.CurrentNotFound())
            : Result.Success(list.ToModel());
    }
}
