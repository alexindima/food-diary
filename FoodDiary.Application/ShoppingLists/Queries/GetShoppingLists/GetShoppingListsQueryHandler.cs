using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.ShoppingLists.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.ShoppingLists.Mappings;
using FoodDiary.Application.ShoppingLists.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Shopping;

namespace FoodDiary.Application.ShoppingLists.Queries.GetShoppingLists;

public sealed class GetShoppingListsQueryHandler(
    IShoppingListReadRepository shoppingListRepository,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetShoppingListsQuery, Result<IReadOnlyList<ShoppingListSummaryModel>>> {
    public async Task<Result<IReadOnlyList<ShoppingListSummaryModel>>> Handle(
        GetShoppingListsQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<IReadOnlyList<ShoppingListSummaryModel>>(userIdResult.Error);
        }

        UserId userId = userIdResult.Value;
        Error? accessError = await currentUserAccessService.EnsureCanAccessAsync(userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<IReadOnlyList<ShoppingListSummaryModel>>(accessError);
        }

        IReadOnlyList<ShoppingList> lists = await shoppingListRepository.GetAllAsync(
            userId,
            includeItems: true,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        var response = lists
            .Select(list => list.ToSummaryModel())
            .ToList();

        return Result.Success<IReadOnlyList<ShoppingListSummaryModel>>(response);
    }
}
