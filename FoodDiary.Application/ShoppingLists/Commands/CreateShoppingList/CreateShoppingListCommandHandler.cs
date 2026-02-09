using System.Threading;
using System.Threading.Tasks;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.ShoppingLists.Mappings;
using FoodDiary.Application.ShoppingLists.Services;
using FoodDiary.Contracts.ShoppingLists;
using FoodDiary.Domain.Entities;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.ShoppingLists.Commands.CreateShoppingList;

public class CreateShoppingListCommandHandler(
    IShoppingListRepository shoppingListRepository,
    IProductRepository productRepository)
    : ICommandHandler<CreateShoppingListCommand, Result<ShoppingListResponse>>
{
    public async Task<Result<ShoppingListResponse>> Handle(
        CreateShoppingListCommand command,
        CancellationToken cancellationToken)
    {
        if (command.UserId is null || command.UserId == UserId.Empty)
        {
            return Result.Failure<ShoppingListResponse>(Errors.Authentication.InvalidToken);
        }

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            return Result.Failure<ShoppingListResponse>(
                Errors.Validation.Required(nameof(command.Name)));
        }

        var itemsResult = await ShoppingListItemBuilder.BuildItemsAsync(
            command.Items,
            command.UserId.Value,
            productRepository,
            cancellationToken);

        if (itemsResult.IsFailure)
        {
            return Result.Failure<ShoppingListResponse>(itemsResult.Error);
        }

        var list = ShoppingList.Create(command.UserId.Value, command.Name);
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

        await shoppingListRepository.AddAsync(list);
        return Result.Success(list.ToResponse());
    }

}
