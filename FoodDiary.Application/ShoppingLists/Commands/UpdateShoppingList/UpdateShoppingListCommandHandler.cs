using System.Threading;
using System.Threading.Tasks;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.ShoppingLists.Mappings;
using FoodDiary.Application.ShoppingLists.Services;
using FoodDiary.Contracts.ShoppingLists;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.ShoppingLists.Commands.UpdateShoppingList;

public class UpdateShoppingListCommandHandler(
    IShoppingListRepository shoppingListRepository,
    IProductRepository productRepository)
    : ICommandHandler<UpdateShoppingListCommand, Result<ShoppingListResponse>>
{
    public async Task<Result<ShoppingListResponse>> Handle(
        UpdateShoppingListCommand command,
        CancellationToken cancellationToken)
    {
        if (command.UserId is null || command.UserId == UserId.Empty)
        {
            return Result.Failure<ShoppingListResponse>(Errors.Authentication.InvalidToken);
        }

        if (string.IsNullOrWhiteSpace(command.Name) && command.Items is null)
        {
            return Result.Failure<ShoppingListResponse>(
                Errors.Validation.Required(nameof(command.Items)));
        }

        var list = await shoppingListRepository.GetByIdAsync(
            command.ShoppingListId,
            command.UserId.Value,
            includeItems: true,
            asTracking: true,
            cancellationToken: cancellationToken);

        if (list is null)
        {
            return Result.Failure<ShoppingListResponse>(Errors.ShoppingList.NotFound(command.ShoppingListId.Value));
        }

        if (!string.IsNullOrWhiteSpace(command.Name))
        {
            list.UpdateName(command.Name);
        }

        if (command.Items is not null)
        {
            var itemsResult = await ShoppingListItemBuilder.BuildItemsAsync(
                command.Items,
                command.UserId.Value,
                productRepository,
                cancellationToken);

            if (itemsResult.IsFailure)
            {
                return Result.Failure<ShoppingListResponse>(itemsResult.Error);
            }

            list.ClearItems();
            foreach (var item in itemsResult.Value)
            {
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
        return Result.Success(list.ToResponse());
    }
}
