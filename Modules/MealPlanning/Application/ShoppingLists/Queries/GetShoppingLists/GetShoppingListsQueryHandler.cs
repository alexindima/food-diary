using FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Mappings;
using FoodDiary.Modules.MealPlanning.Application.Abstractions.ShoppingLists.Common;
using FoodDiary.Modules.MealPlanning.Application.Abstractions.ShoppingLists.Models;
using FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Common;

namespace FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Queries.GetShoppingLists;

public sealed class GetShoppingListsQueryHandler(
    IShoppingListReadModelRepository shoppingListRepository,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetShoppingListsQuery, Result<IReadOnlyList<ShoppingListSummaryModel>>> {
    public async Task<Result<IReadOnlyList<ShoppingListSummaryModel>>> Handle(
        GetShoppingListsQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<IReadOnlyList<ShoppingListSummaryModel>>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        IReadOnlyList<ShoppingListSummaryModel> response = await GetAllAsync(userId, cancellationToken)
            .ConfigureAwait(false);

        return Result.Success(response);
    }
    private async Task<IReadOnlyList<ShoppingListSummaryModel>> GetAllAsync(
        UserId userId,
        CancellationToken cancellationToken) {
        IReadOnlyList<ShoppingListSummaryReadModel> lists = await shoppingListRepository.GetAllSummaryReadModelsAsync(
            userId,
            cancellationToken).ConfigureAwait(false);

        return lists
            .Select(list => list.ToSummaryModel())
            .ToList();
    }
}
