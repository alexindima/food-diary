using FoodDiary.Modules.Users.Contracts.Common.Validation;
using FoodDiary.Modules.MealPlanning.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.MealPlanning.Application.Common.Validation;
using FoodDiary.Modules.MealPlanning.Application.Abstractions.ShoppingLists.Common;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.MealPlanning.Domain.Entities.Shopping;

namespace FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Commands.DeleteShoppingList;

public sealed class DeleteShoppingListCommandHandler(
    IShoppingListWriteRepository shoppingListRepository,
    ICurrentUserAccessService currentUserAccessService)
    : ICommandHandler<DeleteShoppingListCommand, Result> {
    public async Task<Result> Handle(
        DeleteShoppingListCommand command,
        CancellationToken cancellationToken) {
        Result<ShoppingListId> shoppingListIdResult = RequiredIdParser.Parse(
            command.ShoppingListId,
            nameof(command.ShoppingListId),
            "Shopping list id must not be empty.",
            value => new ShoppingListId(value));
        if (shoppingListIdResult.IsFailure) {
            return RequiredIdParser.ToFailure(shoppingListIdResult);
        }

        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure(userIdResult);
        }

        UserId userId = userIdResult.Value;
        ShoppingListId shoppingListId = shoppingListIdResult.Value;

        ShoppingList? list = await shoppingListRepository.GetByIdAsync(
            shoppingListId,
            userId,
            includeItems: true,
            asTracking: true,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (list is null) {
            return Result.Failure(ShoppingListErrors.NotFound(command.ShoppingListId));
        }

        await shoppingListRepository.DeleteAsync(list, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
