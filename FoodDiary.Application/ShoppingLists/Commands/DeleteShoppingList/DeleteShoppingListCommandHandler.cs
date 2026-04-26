using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.ShoppingLists.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.ShoppingLists.Commands.DeleteShoppingList;

public class DeleteShoppingListCommandHandler(
    IShoppingListRepository shoppingListRepository,
    IUserRepository userRepository)
    : ICommandHandler<DeleteShoppingListCommand, Result> {
    public async Task<Result> Handle(
        DeleteShoppingListCommand command,
        CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure(Errors.Authentication.InvalidToken);
        }

        if (command.ShoppingListId == Guid.Empty) {
            return Result.Failure(Errors.Validation.Invalid(nameof(command.ShoppingListId), "Shopping list id must not be empty."));
        }

        var userId = new UserId(command.UserId!.Value);
        var accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken);
        if (accessError is not null) {
            return Result.Failure(accessError);
        }
        var shoppingListId = new ShoppingListId(command.ShoppingListId);

        var list = await shoppingListRepository.GetByIdAsync(
            shoppingListId,
            userId,
            includeItems: true,
            asTracking: true,
            cancellationToken: cancellationToken);

        if (list is null) {
            return Result.Failure(Errors.ShoppingList.NotFound(command.ShoppingListId));
        }

        await shoppingListRepository.DeleteAsync(list, cancellationToken);
        return Result.Success();
    }
}
