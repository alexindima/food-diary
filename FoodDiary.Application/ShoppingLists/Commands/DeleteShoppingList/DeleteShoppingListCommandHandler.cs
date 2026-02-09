using System.Threading;
using System.Threading.Tasks;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;

namespace FoodDiary.Application.ShoppingLists.Commands.DeleteShoppingList;

public class DeleteShoppingListCommandHandler(IShoppingListRepository shoppingListRepository)
    : ICommandHandler<DeleteShoppingListCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        DeleteShoppingListCommand command,
        CancellationToken cancellationToken)
    {
        var list = await shoppingListRepository.GetByIdAsync(
            command.ShoppingListId,
            command.UserId!.Value,
            includeItems: true,
            asTracking: true,
            cancellationToken: cancellationToken);

        if (list is null)
        {
            return Result.Failure<bool>(Errors.ShoppingList.NotFound(command.ShoppingListId.Value));
        }

        await shoppingListRepository.DeleteAsync(list);
        return Result.Success(true);
    }
}
