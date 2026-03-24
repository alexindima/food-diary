using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.ShoppingLists.Mappings;
using FoodDiary.Application.ShoppingLists.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.ShoppingLists.Queries.GetShoppingListById;

public class GetShoppingListByIdQueryHandler(IShoppingListRepository shoppingListRepository)
    : IQueryHandler<GetShoppingListByIdQuery, Result<ShoppingListModel>> {
    public async Task<Result<ShoppingListModel>> Handle(
        GetShoppingListByIdQuery query,
        CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<ShoppingListModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(query.UserId.Value);
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
