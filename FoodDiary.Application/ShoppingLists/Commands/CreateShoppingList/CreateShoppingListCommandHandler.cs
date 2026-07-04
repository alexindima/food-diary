using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Application.Abstractions.ShoppingLists.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.ShoppingLists.Mappings;
using FoodDiary.Application.ShoppingLists.Models;
using FoodDiary.Application.ShoppingLists.Services;
using FoodDiary.Domain.Entities.Shopping;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.ShoppingLists.Commands.CreateShoppingList;

public sealed class CreateShoppingListCommandHandler(
    IShoppingListWriteRepository shoppingListRepository,
    IProductLookupService productLookupService,
    ICurrentUserAccessService currentUserAccessService)
    : ICommandHandler<CreateShoppingListCommand, Result<ShoppingListModel>> {
    public async Task<Result<ShoppingListModel>> Handle(
        CreateShoppingListCommand command,
        CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<ShoppingListModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId!.Value);
        Error? accessError = await currentUserAccessService.EnsureCanAccessAsync(userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<ShoppingListModel>(accessError);
        }

        if (string.IsNullOrWhiteSpace(command.Name)) {
            return Result.Failure<ShoppingListModel>(
                Errors.Validation.Required(nameof(command.Name)));
        }

        Result<IReadOnlyList<ShoppingListItemData>> itemsResult = await ShoppingListItemBuilder.BuildItemsAsync(
            command.Items,
            userId,
            productLookupService,
            cancellationToken).ConfigureAwait(false);

        if (itemsResult.IsFailure) {
            return Result.Failure<ShoppingListModel>(itemsResult.Error);
        }

        var list = ShoppingList.Create(userId, command.Name);
        foreach (ShoppingListItemData item in itemsResult.Value) {
            list.AddItem(
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
        }

        await shoppingListRepository.AddAsync(list, cancellationToken).ConfigureAwait(false);
        return Result.Success(list.ToModel());
    }
}
