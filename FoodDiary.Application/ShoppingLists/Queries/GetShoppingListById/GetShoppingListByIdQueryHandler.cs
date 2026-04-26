using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.ShoppingLists.Common;
using FoodDiary.Application.ShoppingLists.Mappings;
using FoodDiary.Application.ShoppingLists.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

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
        var accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken);
        if (accessError is not null) {
            return Result.Failure<ShoppingListModel>(accessError);
        }

        var shoppingListId = new ShoppingListId(query.ShoppingListId);

        var list = await shoppingListRepository.GetByIdAsync(
            shoppingListId,
            userId,
            includeItems: true,
            cancellationToken: cancellationToken);

        return list is null
            ? Result.Failure<ShoppingListModel>(Errors.ShoppingList.NotFound(query.ShoppingListId))
            : Result.Success(list.ToModel());
    }
}
