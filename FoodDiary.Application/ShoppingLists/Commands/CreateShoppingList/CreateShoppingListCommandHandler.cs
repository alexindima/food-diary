using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Products.Common;
using FoodDiary.Application.ShoppingLists.Common;
using FoodDiary.Application.ShoppingLists.Mappings;
using FoodDiary.Application.ShoppingLists.Models;
using FoodDiary.Application.ShoppingLists.Services;
using FoodDiary.Domain.Entities.Shopping;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.ShoppingLists.Commands.CreateShoppingList;

public class CreateShoppingListCommandHandler(
    IShoppingListRepository shoppingListRepository,
    IProductLookupService productLookupService)
    : ICommandHandler<CreateShoppingListCommand, Result<ShoppingListModel>> {
    public async Task<Result<ShoppingListModel>> Handle(
        CreateShoppingListCommand command,
        CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<ShoppingListModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId!.Value);

        if (string.IsNullOrWhiteSpace(command.Name)) {
            return Result.Failure<ShoppingListModel>(
                Errors.Validation.Required(nameof(command.Name)));
        }

        var itemsResult = await ShoppingListItemBuilder.BuildItemsAsync(
            command.Items,
            userId,
            productLookupService,
            cancellationToken);

        if (itemsResult.IsFailure) {
            return Result.Failure<ShoppingListModel>(itemsResult.Error);
        }

        var list = ShoppingList.Create(userId, command.Name);
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

        await shoppingListRepository.AddAsync(list, cancellationToken);
        return Result.Success(list.ToModel());
    }
}
