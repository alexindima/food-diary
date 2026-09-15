using FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Mappings;
using FoodDiary.Modules.MealPlanning.Domain.ValueObjects.Ids;
using FoodDiary.Modules.MealPlanning.Application.Abstractions.ShoppingLists.Common;
using FoodDiary.Modules.MealPlanning.Application.Abstractions.ShoppingLists.Models;
using FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.MealPlanning.Application.Common.Validation;

namespace FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Queries.GetShoppingListById;

public sealed class GetShoppingListByIdQueryHandler(
    IShoppingListReadModelRepository shoppingListRepository,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetShoppingListByIdQuery, Result<ShoppingListModel>> {
    public async Task<Result<ShoppingListModel>> Handle(
        GetShoppingListByIdQuery query,
        CancellationToken cancellationToken) {
        Result<ShoppingListId> shoppingListIdResult = RequiredIdParser.Parse(
            query.ShoppingListId,
            nameof(query.ShoppingListId),
            "Shopping list id must not be empty.",
            value => new ShoppingListId(value));
        if (shoppingListIdResult.IsFailure) {
            return RequiredIdParser.ToFailure<ShoppingListModel, ShoppingListId>(shoppingListIdResult);
        }

        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<ShoppingListModel>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        ShoppingListId shoppingListId = shoppingListIdResult.Value;

        ShoppingListModel? list = await GetByIdAsync(
            shoppingListId,
            userId,
            cancellationToken).ConfigureAwait(false);

        return list is null
            ? Result.Failure<ShoppingListModel>(ShoppingListErrors.NotFound(query.ShoppingListId))
            : Result.Success(list);
    }
    private async Task<ShoppingListModel?> GetByIdAsync(
        ShoppingListId shoppingListId,
        UserId userId,
        CancellationToken cancellationToken) {
        ShoppingListReadModel? list = await shoppingListRepository.GetReadModelByIdAsync(
            shoppingListId,
            userId,
            cancellationToken).ConfigureAwait(false);

        return list?.ToModel();
    }
}
