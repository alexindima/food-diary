using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.MealPlanning.Application.Abstractions.ShoppingLists.Common;
using FoodDiary.Modules.MealPlanning.Application.Abstractions.ShoppingLists.Models;
using FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Mappings;
using FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Models;
using FoodDiary.Modules.MealPlanning.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Queries.GetShoppingListOverview;

public sealed class GetShoppingListOverviewQueryHandler(
    IShoppingListReadModelRepository repository,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetShoppingListOverviewQuery, Result<ShoppingListOverviewModel>> {
    private const int PageSize = 20;
    public async Task<Result<ShoppingListOverviewModel>> Handle(GetShoppingListOverviewQuery query, CancellationToken cancellationToken) {
        Result<UserId> user = await CurrentUserAccessResolver.ResolveAsync(query.UserId, currentUserAccessService, cancellationToken).ConfigureAwait(false);
        if (user.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<ShoppingListOverviewModel>(user);
        }
        IReadOnlyList<ShoppingListSummaryReadModel> rows = await repository.GetAllSummaryReadModelsAsync(
            user.Value, cancellationToken, pageSize: PageSize + 1).ConfigureAwait(false);
        bool hasMore = rows.Count > PageSize;
        ShoppingListModel? selected = null;
        if (rows.Count > 0) {
            var id = new ShoppingListId(rows[0].Id);
            ShoppingListReadModel? detail = await repository.GetReadModelByIdAsync(id, user.Value, cancellationToken).ConfigureAwait(false);
            selected = detail?.ToModel();
        }
        var page = new ShoppingListPageModel(rows.Take(PageSize).Select(row => row.ToSummaryModel()).ToList(), hasMore, hasMore ? 2 : null);
        return Result.Success(new ShoppingListOverviewModel(selected, page));
    }
}
