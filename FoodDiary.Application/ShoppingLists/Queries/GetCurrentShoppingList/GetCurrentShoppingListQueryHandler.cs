using System.Threading;
using System.Threading.Tasks;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.ShoppingLists.Mappings;
using FoodDiary.Contracts.ShoppingLists;

namespace FoodDiary.Application.ShoppingLists.Queries.GetCurrentShoppingList;

public class GetCurrentShoppingListQueryHandler(IShoppingListRepository shoppingListRepository)
    : IQueryHandler<GetCurrentShoppingListQuery, Result<ShoppingListResponse>>
{
    public async Task<Result<ShoppingListResponse>> Handle(
        GetCurrentShoppingListQuery query,
        CancellationToken cancellationToken)
    {
        var list = await shoppingListRepository.GetCurrentAsync(
            query.UserId!.Value,
            includeItems: true,
            cancellationToken: cancellationToken);

        if (list is null)
        {
            return Result.Failure<ShoppingListResponse>(Errors.ShoppingList.CurrentNotFound());
        }

        return Result.Success(list.ToResponse());
    }
}
