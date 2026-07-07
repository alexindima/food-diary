using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Application.Abstractions.ShoppingLists.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.ShoppingLists.Mappings;
using FoodDiary.Application.ShoppingLists.Models;
using FoodDiary.Application.ShoppingLists.Services;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Shopping;

namespace FoodDiary.Application.ShoppingLists.Commands.UpdateShoppingList;

public sealed class UpdateShoppingListCommandHandler(
    IShoppingListWriteRepository shoppingListRepository,
    IProductLookupService productLookupService,
    ICurrentUserAccessService currentUserAccessService)
    : ICommandHandler<UpdateShoppingListCommand, Result<ShoppingListModel>> {
    public async Task<Result<ShoppingListModel>> Handle(
        UpdateShoppingListCommand command,
        CancellationToken cancellationToken) {
        Result<UserId> validationResult = await ValidateCommandAsync(command, cancellationToken).ConfigureAwait(false);
        if (validationResult.IsFailure) {
            return Result.Failure<ShoppingListModel>(validationResult.Error);
        }

        UserId userId = validationResult.Value;
        var shoppingListId = new ShoppingListId(command.ShoppingListId);
        ShoppingList? list = await shoppingListRepository.GetByIdAsync(
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

        Result itemsResult = await ApplyItemsAsync(command, userId, list, cancellationToken).ConfigureAwait(false);
        if (itemsResult.IsFailure) {
            return Result.Failure<ShoppingListModel>(itemsResult.Error);
        }

        await shoppingListRepository.UpdateAsync(list, cancellationToken).ConfigureAwait(false);
        return Result.Success(list.ToModel());
    }

    private static Result ValidateCommandShape(UpdateShoppingListCommand command) {
        if (command.ShoppingListId == Guid.Empty) {
            return Result.Failure(
                Errors.Validation.Invalid(nameof(command.ShoppingListId), "Shopping list id must not be empty."));
        }

        if (string.IsNullOrWhiteSpace(command.Name) && command.Items is null) {
            return Result.Failure(Errors.Validation.Required(nameof(command.Items)));
        }

        return Result.Success();
    }

    private async Task<Result<UserId>> ValidateCommandAsync(UpdateShoppingListCommand command, CancellationToken cancellationToken) {
        Result commandResult = ValidateCommandShape(command);
        if (commandResult.IsFailure) {
            return Result.Failure<UserId>(commandResult.Error);
        }

        return await CurrentUserAccessResolver.ResolveAsync(
            command.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<Result> ApplyItemsAsync(
        UpdateShoppingListCommand command,
        UserId userId,
        ShoppingList list,
        CancellationToken cancellationToken) {
        if (command.Items is null) {
            return Result.Success();
        }

        Result<IReadOnlyList<ShoppingListItemData>> itemsResult = await ShoppingListItemBuilder.BuildItemsAsync(
            command.Items,
            userId,
            productLookupService,
            cancellationToken).ConfigureAwait(false);

        if (itemsResult.IsFailure) {
            return Result.Failure(itemsResult.Error);
        }

        var retainedItemIds = new HashSet<ShoppingListItemId>();
        foreach (ShoppingListItemData item in itemsResult.Value) {
            ShoppingListItem? existing = item.Id.HasValue ? list.FindItem(item.Id.Value) : null;
            if (existing is null) {
                ShoppingListItem added = list.AddItem(
                    item.Name,
                    item.ProductId,
                    item.Amount,
                    item.Unit,
                    item.Category,
                    item.IsChecked,
                    item.SortOrder,
                    item.Aisle,
                    item.Note,
                    item.CheckedOnUtc,
                    item.Id);
                retainedItemIds.Add(added.Id);
                continue;
            }

            existing.UpdateDetails(
                item.Name,
                item.ProductId,
                item.Amount,
                item.Unit,
                item.Category,
                item.Aisle,
                item.Note,
                item.IsChecked,
                item.CheckedOnUtc,
                item.SortOrder);
            retainedItemIds.Add(existing.Id);
        }

        list.RemoveItemsExcept(retainedItemIds);

        return Result.Success();
    }
}
