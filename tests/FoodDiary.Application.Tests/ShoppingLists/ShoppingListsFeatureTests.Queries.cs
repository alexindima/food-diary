using FoodDiary.Application.ShoppingLists.Mappings;
using FoodDiary.Application.ShoppingLists.Queries.GetCurrentShoppingList;
using FoodDiary.Application.ShoppingLists.Queries.GetShoppingListById;
using FoodDiary.Application.ShoppingLists.Queries.GetShoppingLists;
using FoodDiary.Domain.Entities.Shopping;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Results;
using FoodDiary.Application.ShoppingLists.Models;

namespace FoodDiary.Application.Tests.ShoppingLists;

public partial class ShoppingListsFeatureTests {

    [Fact]
    public async Task GetCurrentShoppingListQueryHandler_WithMissingUserId_ReturnsInvalidToken() {
        GetCurrentShoppingListQueryHandler handler = CreateCurrentShoppingListHandler(new NoopShoppingListRepository(), CreateCurrentUserAccessService(User.Create("user@example.com", "hash")));
        Result<ShoppingListModel> result = await handler.Handle(new GetCurrentShoppingListQuery(UserId: null), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetCurrentShoppingListQueryHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-current-shopping@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        GetCurrentShoppingListQueryHandler handler = CreateCurrentShoppingListHandler(new NoopShoppingListRepository(), CreateCurrentUserAccessService(user));

        Result<ShoppingListModel> result = await handler.Handle(new GetCurrentShoppingListQuery(user.Id.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task GetShoppingListByIdQueryHandler_WithMissingUserId_ReturnsInvalidToken() {
        GetShoppingListByIdQueryHandler handler = CreateShoppingListByIdHandler(new NoopShoppingListRepository(), CreateCurrentUserAccessService(User.Create("user@example.com", "hash")));
        Result<ShoppingListModel> result = await handler.Handle(new GetShoppingListByIdQuery(UserId: null, Guid.NewGuid()), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetShoppingListByIdQueryHandler_WithEmptyShoppingListId_ReturnsValidationFailure() {
        GetShoppingListByIdQueryHandler handler = CreateShoppingListByIdHandler(new NoopShoppingListRepository(), CreateCurrentUserAccessService(User.Create("user@example.com", "hash")));
        Result<ShoppingListModel> result = await handler.Handle(new GetShoppingListByIdQuery(Guid.NewGuid(), Guid.Empty), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ShoppingListId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetShoppingListByIdQueryHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-shopping-by-id@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        GetShoppingListByIdQueryHandler handler = CreateShoppingListByIdHandler(new NoopShoppingListRepository(), CreateCurrentUserAccessService(user));

        Result<ShoppingListModel> result = await handler.Handle(new GetShoppingListByIdQuery(user.Id.Value, Guid.NewGuid()), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task GetShoppingListByIdQueryHandler_WhenListMissing_ReturnsNotFound() {
        var user = User.Create("shopping-by-id-missing@example.com", "hash");
        GetShoppingListByIdQueryHandler handler = CreateShoppingListByIdHandler(new NoopShoppingListRepository(), CreateCurrentUserAccessService(user));
        var shoppingListId = Guid.NewGuid();

        Result<ShoppingListModel> result = await handler.Handle(new GetShoppingListByIdQuery(user.Id.Value, shoppingListId), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("ShoppingList.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task GetShoppingListByIdQueryHandler_WithList_ReturnsModel() {
        var user = User.Create("shopping-by-id-success@example.com", "hash");
        var list = ShoppingList.Create(user.Id, "Weekly");
        list.AddItem("Milk", productId: null, 1, MeasurementUnit.Ml, "Dairy", isChecked: false, 1);
        GetShoppingListByIdQueryHandler handler = CreateShoppingListByIdHandler(new SingleShoppingListRepository(list), CreateCurrentUserAccessService(user));

        Result<ShoppingListModel> result = await handler.Handle(new GetShoppingListByIdQuery(user.Id.Value, list.Id.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal("Weekly", result.Value.Name);
        Assert.Single(result.Value.Items);
    }

    [Fact]
    public async Task GetShoppingListsQueryHandler_WithMissingUserId_ReturnsInvalidToken() {
        GetShoppingListsQueryHandler handler = CreateShoppingListsHandler(new NoopShoppingListRepository(), CreateCurrentUserAccessService(User.Create("user@example.com", "hash")));
        Result<IReadOnlyList<ShoppingListSummaryModel>> result = await handler.Handle(new GetShoppingListsQuery(UserId: null), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetShoppingListsQueryHandler_WithLists_ReturnsSummaryModels() {
        var user = User.Create("shopping-lists-success@example.com", "hash");
        var list = ShoppingList.Create(user.Id, "Weekly");
        list.AddItem("Milk", productId: null, 1, MeasurementUnit.Ml, "Dairy", isChecked: false, 1);
        GetShoppingListsQueryHandler handler = CreateShoppingListsHandler(new SingleShoppingListRepository(list), CreateCurrentUserAccessService(user));

        Result<IReadOnlyList<ShoppingListSummaryModel>> result = await handler.Handle(new GetShoppingListsQuery(user.Id.Value), CancellationToken.None);

        ResultAssert.Success(result);
        ShoppingListSummaryModel summary = Assert.Single(result.Value);
        Assert.Equal(list.Id.Value, summary.Id);
        Assert.Equal("Weekly", summary.Name);
        Assert.Equal(1, summary.ItemsCount);
    }

    [Fact]
    public async Task GetShoppingListsQueryHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-shopping@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        GetShoppingListsQueryHandler handler = CreateShoppingListsHandler(new NoopShoppingListRepository(), CreateCurrentUserAccessService(user));

        Result<IReadOnlyList<ShoppingListSummaryModel>> result = await handler.Handle(new GetShoppingListsQuery(user.Id.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }
}
