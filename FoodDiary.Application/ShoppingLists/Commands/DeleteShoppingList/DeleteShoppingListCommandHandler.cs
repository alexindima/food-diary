using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.ShoppingLists.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.ShoppingLists.Commands.DeleteShoppingList;

public class DeleteShoppingListCommandHandler(IShoppingListRepository shoppingListRepository)
    : ICommandHandler<DeleteShoppingListCommand, Result<bool>> {
    public async Task<Result<bool>> Handle(
        DeleteShoppingListCommand command,
        CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<bool>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId!.Value);
        var shoppingListId = new ShoppingListId(command.ShoppingListId);

        var list = await shoppingListRepository.GetByIdAsync(
            shoppingListId,
            userId,
            includeItems: true,
            asTracking: true,
            cancellationToken: cancellationToken);

        if (list is null) {
            return Result.Failure<bool>(Errors.ShoppingList.NotFound(command.ShoppingListId));
        }

        await shoppingListRepository.DeleteAsync(list, cancellationToken);
        return Result.Success(true);
    }
}
