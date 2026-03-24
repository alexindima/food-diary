using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.ShoppingLists.Mappings;
using FoodDiary.Application.ShoppingLists.Models;
using FoodDiary.Application.ShoppingLists.Services;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.ShoppingLists.Commands.UpdateShoppingList;

public class UpdateShoppingListCommandHandler(
    IShoppingListRepository shoppingListRepository,
    IProductRepository productRepository)
    : ICommandHandler<UpdateShoppingListCommand, Result<ShoppingListModel>> {
    public async Task<Result<ShoppingListModel>> Handle(
        UpdateShoppingListCommand command,
        CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<ShoppingListModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId.Value);

        if (string.IsNullOrWhiteSpace(command.Name) && command.Items is null) {
            return Result.Failure<ShoppingListModel>(
                Errors.Validation.Required(nameof(command.Items)));
        }

        var shoppingListId = new ShoppingListId(command.ShoppingListId);

        var list = await shoppingListRepository.GetByIdAsync(
            shoppingListId,
            userId,
            includeItems: true,
            asTracking: true,
            cancellationToken: cancellationToken);

        if (list is null) {
            return Result.Failure<ShoppingListModel>(Errors.ShoppingList.NotFound(command.ShoppingListId));
        }

        if (!string.IsNullOrWhiteSpace(command.Name)) {
            list.UpdateName(command.Name);
        }

        if (command.Items is not null) {
            var itemsResult = await ShoppingListItemBuilder.BuildItemsAsync(
                command.Items,
                userId,
                productRepository,
                cancellationToken);

            if (itemsResult.IsFailure) {
                return Result.Failure<ShoppingListModel>(itemsResult.Error);
            }

            list.ClearItems();
            foreach (var item in itemsResult.Value) {
                list.AddItem(
                    item.Name,
                    item.ProductId,
                    item.Amount,
                    item.Unit,
                    item.Category,
                    item.IsChecked,
                    item.SortOrder);
            }
        }

        await shoppingListRepository.UpdateAsync(list);
        return Result.Success(list.ToModel());
    }
}
