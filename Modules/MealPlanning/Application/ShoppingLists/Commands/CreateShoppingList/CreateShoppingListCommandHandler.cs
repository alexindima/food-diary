using FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Mappings;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Products.Contracts.Common;
using FoodDiary.Modules.MealPlanning.Application.Abstractions.ShoppingLists.Common;
using FoodDiary.Modules.Users.Contracts.Common;

using FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Common;
using FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Models;
using FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Services;
using FoodDiary.Modules.MealPlanning.Domain.Entities.Shopping;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Commands.CreateShoppingList;

public sealed class CreateShoppingListCommandHandler(
    IShoppingListWriteRepository shoppingListRepository,
    IProductLookupService productLookupService,
    ICurrentUserAccessService currentUserAccessService)
    : ICommandHandler<CreateShoppingListCommand, Result<ShoppingListModel>> {
    public async Task<Result<ShoppingListModel>> Handle(
        CreateShoppingListCommand command,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<ShoppingListModel>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        if (string.IsNullOrWhiteSpace(command.Name)) {
            return Result.Failure<ShoppingListModel>(
                Errors.Validation.Required(nameof(command.Name)));
        }

        if (command.Name.Trim().Length > ShoppingListInputLimits.NameMaxLength) {
            return Result.Failure<ShoppingListModel>(
                Errors.Validation.Invalid(nameof(command.Name), $"Name must be at most {ShoppingListInputLimits.NameMaxLength} characters."));
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
