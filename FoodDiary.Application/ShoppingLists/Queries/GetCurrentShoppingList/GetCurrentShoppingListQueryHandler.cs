using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.ShoppingLists.Common;
using FoodDiary.Application.ShoppingLists.Mappings;
using FoodDiary.Application.ShoppingLists.Models;
using FoodDiary.Application.Users.Common;

namespace FoodDiary.Application.ShoppingLists.Queries.GetCurrentShoppingList;

public class GetCurrentShoppingListQueryHandler(
    IShoppingListRepository shoppingListRepository,
    IUserRepository userRepository)
    : IQueryHandler<GetCurrentShoppingListQuery, Result<ShoppingListModel>> {
    public async Task<Result<ShoppingListModel>> Handle(
        GetCurrentShoppingListQuery query,
        CancellationToken cancellationToken) {
        var userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<ShoppingListModel>(userIdResult.Error);
        }

        var userId = userIdResult.Value;
        var accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken);
        if (accessError is not null) {
            return Result.Failure<ShoppingListModel>(accessError);
        }

        var list = await shoppingListRepository.GetCurrentAsync(
            userId,
            includeItems: true,
            cancellationToken: cancellationToken);

        return list is null
            ? Result.Failure<ShoppingListModel>(Errors.ShoppingList.CurrentNotFound())
            : Result.Success(list.ToModel());
    }
}
