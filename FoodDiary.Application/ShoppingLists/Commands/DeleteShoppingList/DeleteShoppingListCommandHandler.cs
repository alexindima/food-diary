using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.ShoppingLists.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Shopping;

namespace FoodDiary.Application.ShoppingLists.Commands.DeleteShoppingList;

public sealed class DeleteShoppingListCommandHandler(
    IShoppingListWriteRepository shoppingListRepository,
    ICurrentUserAccessService currentUserAccessService)
    : ICommandHandler<DeleteShoppingListCommand, Result> {
    public async Task<Result> Handle(
        DeleteShoppingListCommand command,
        CancellationToken cancellationToken) {
        if (command.ShoppingListId == Guid.Empty) {
            return Result.Failure(Errors.Validation.Invalid(nameof(command.ShoppingListId), "Shopping list id must not be empty."));
        }

        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return Result.Failure(userIdResult.Error);
        }

        UserId userId = userIdResult.Value;
        var shoppingListId = new ShoppingListId(command.ShoppingListId);

        ShoppingList? list = await shoppingListRepository.GetByIdAsync(
            shoppingListId,
            userId,
            includeItems: true,
            asTracking: true,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (list is null) {
            return Result.Failure(Errors.ShoppingList.NotFound(command.ShoppingListId));
        }

        await shoppingListRepository.DeleteAsync(list, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
