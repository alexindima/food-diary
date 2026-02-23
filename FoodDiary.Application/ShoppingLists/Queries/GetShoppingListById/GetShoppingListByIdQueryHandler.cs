using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.ShoppingLists.Mappings;
using FoodDiary.Contracts.ShoppingLists;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.ShoppingLists.Queries.GetShoppingListById;

public class GetShoppingListByIdQueryHandler(IShoppingListRepository shoppingListRepository)
    : IQueryHandler<GetShoppingListByIdQuery, Result<ShoppingListResponse>> {
    public async Task<Result<ShoppingListResponse>> Handle(
        GetShoppingListByIdQuery query,
        CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == UserId.Empty) {
            return Result.Failure<ShoppingListResponse>(Errors.Authentication.InvalidToken);
        }

        var list = await shoppingListRepository.GetByIdAsync(
            query.ShoppingListId,
            query.UserId.Value,
            includeItems: true,
            cancellationToken: cancellationToken);

        return list is null
            ? Result.Failure<ShoppingListResponse>(Errors.ShoppingList.NotFound(query.ShoppingListId.Value))
            : Result.Success(list.ToResponse());
    }
}
