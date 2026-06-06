using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.ShoppingLists.Common;
using FoodDiary.Application.ShoppingLists.Mappings;
using FoodDiary.Application.ShoppingLists.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Shopping;

namespace FoodDiary.Application.ShoppingLists.Queries.GetShoppingListById;

public class GetShoppingListByIdQueryHandler(
    IShoppingListRepository shoppingListRepository,
    IUserRepository userRepository)
    : IQueryHandler<GetShoppingListByIdQuery, Result<ShoppingListModel>> {
    public async Task<Result<ShoppingListModel>> Handle(
        GetShoppingListByIdQuery query,
        CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<ShoppingListModel>(Errors.Authentication.InvalidToken);
        }

        if (query.ShoppingListId == Guid.Empty) {
            return Result.Failure<ShoppingListModel>(
                Errors.Validation.Invalid(nameof(query.ShoppingListId), "Shopping list id must not be empty."));
        }

        var userId = new UserId(query.UserId!.Value);
        Error? accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<ShoppingListModel>(accessError);
        }

        var shoppingListId = new ShoppingListId(query.ShoppingListId);

        ShoppingList? list = await shoppingListRepository.GetByIdAsync(
            shoppingListId,
            userId,
            includeItems: true,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return list is null
            ? Result.Failure<ShoppingListModel>(Errors.ShoppingList.NotFound(query.ShoppingListId))
            : Result.Success(list.ToModel());
    }
}
