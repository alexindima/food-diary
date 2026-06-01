using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Application.Abstractions.ShoppingLists.Common;
using FoodDiary.Application.ShoppingLists.Mappings;
using FoodDiary.Application.ShoppingLists.Models;
using FoodDiary.Application.ShoppingLists.Services;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.ShoppingLists.Commands.UpdateShoppingList;

public class UpdateShoppingListCommandHandler(
    IShoppingListRepository shoppingListRepository,
    IProductLookupService productLookupService,
    IUserRepository userRepository)
    : ICommandHandler<UpdateShoppingListCommand, Result<ShoppingListModel>> {
    public async Task<Result<ShoppingListModel>> Handle(
        UpdateShoppingListCommand command,
        CancellationToken cancellationToken) {
        var validationResult = await ValidateCommandAsync(command, cancellationToken).ConfigureAwait(false);
        if (validationResult.IsFailure) {
            return Result.Failure<ShoppingListModel>(validationResult.Error);
        }

        var userId = validationResult.Value;
        var shoppingListId = new ShoppingListId(command.ShoppingListId);
        var list = await shoppingListRepository.GetByIdAsync(
            shoppingListId,
            userId,
            includeItems: true,
            asTracking: true,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (list is null) {
            return Result.Failure<ShoppingListModel>(Errors.ShoppingList.NotFound(command.ShoppingListId));
        }

        if (!string.IsNullOrWhiteSpace(command.Name)) {
            list.UpdateName(command.Name);
        }

        var itemsResult = await ApplyItemsAsync(command, userId, list, cancellationToken).ConfigureAwait(false);
        if (itemsResult.IsFailure) {
            return Result.Failure<ShoppingListModel>(itemsResult.Error);
        }

        await shoppingListRepository.UpdateAsync(list, cancellationToken).ConfigureAwait(false);
        return Result.Success(list.ToModel());
    }

    private static Result<UserId> ValidateUser(UpdateShoppingListCommand command) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<UserId>(Errors.Authentication.InvalidToken);
        }

        if (command.ShoppingListId == Guid.Empty) {
            return Result.Failure<UserId>(
                Errors.Validation.Invalid(nameof(command.ShoppingListId), "Shopping list id must not be empty."));
        }

        if (string.IsNullOrWhiteSpace(command.Name) && command.Items is null) {
            return Result.Failure<UserId>(Errors.Validation.Required(nameof(command.Items)));
        }

        return Result.Success(new UserId(command.UserId.Value));
    }

    private async Task<Result<UserId>> ValidateCommandAsync(UpdateShoppingListCommand command, CancellationToken cancellationToken) {
        var userResult = ValidateUser(command);
        if (userResult.IsFailure) {
            return userResult;
        }

        var accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userResult.Value, cancellationToken).ConfigureAwait(false);
        return accessError is null
            ? userResult
            : Result.Failure<UserId>(accessError);
    }

    private async Task<Result> ApplyItemsAsync(
        UpdateShoppingListCommand command,
        UserId userId,
        Domain.Entities.Shopping.ShoppingList list,
        CancellationToken cancellationToken) {
        if (command.Items is null) {
            return Result.Success();
        }

        var itemsResult = await ShoppingListItemBuilder.BuildItemsAsync(
            command.Items,
            userId,
            productLookupService,
            cancellationToken).ConfigureAwait(false);

        if (itemsResult.IsFailure) {
            return Result.Failure(itemsResult.Error);
        }

        list.ClearItems();
        foreach (var item in itemsResult.Value) {
            list.AddItem(item.Name, item.ProductId, item.Amount, item.Unit, item.Category, item.IsChecked, item.SortOrder);
        }

        return Result.Success();
    }
}
