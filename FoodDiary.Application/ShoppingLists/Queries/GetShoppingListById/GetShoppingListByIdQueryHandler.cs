using System.Threading;
using System.Threading.Tasks;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.ShoppingLists.Mappings;
using FoodDiary.Contracts.ShoppingLists;

namespace FoodDiary.Application.ShoppingLists.Queries.GetShoppingListById;

public class GetShoppingListByIdQueryHandler(IShoppingListRepository shoppingListRepository)
    : IQueryHandler<GetShoppingListByIdQuery, Result<ShoppingListResponse>>
{
    public async Task<Result<ShoppingListResponse>> Handle(
        GetShoppingListByIdQuery query,
        CancellationToken cancellationToken)
    {
        var list = await shoppingListRepository.GetByIdAsync(
            query.ShoppingListId,
            query.UserId!.Value,
            includeItems: true,
            cancellationToken: cancellationToken);

        if (list is null)
        {
            return Result.Failure<ShoppingListResponse>(Errors.ShoppingList.NotFound(query.ShoppingListId.Value));
        }

        return Result.Success(list.ToResponse());
    }
}
