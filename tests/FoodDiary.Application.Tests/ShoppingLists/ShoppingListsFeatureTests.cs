using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.ShoppingLists.Common;
using FoodDiary.Application.ShoppingLists.Commands.CreateShoppingList;
using FoodDiary.Application.ShoppingLists.Commands.DeleteShoppingList;
using FoodDiary.Application.ShoppingLists.Commands.UpdateShoppingList;
using FoodDiary.Application.Abstractions.ShoppingLists.Common;
using FoodDiary.Application.Abstractions.ShoppingLists.Models;
using FoodDiary.Application.ShoppingLists.Mappings;
using FoodDiary.Application.ShoppingLists.Queries.GetCurrentShoppingList;
using FoodDiary.Application.ShoppingLists.Queries.GetShoppingListById;
using FoodDiary.Application.ShoppingLists.Queries.GetShoppingLists;
using FoodDiary.Application.ShoppingLists.Services;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Shopping;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;
using FoodDiary.Application.ShoppingLists.Models;

namespace FoodDiary.Application.Tests.ShoppingLists;

[ExcludeFromCodeCoverage]
public class ShoppingListsFeatureTests {
    private static ShoppingListReadModel ToReadModel(ShoppingList list) =>
        new(
            list.Id.Value,
            list.Name,
            list.CreatedOnUtc,
            [.. list.Items
                .OrderBy(item => item.SortOrder)
                .ThenBy(item => item.Id.Value)
                .Select(item => new ShoppingListItemReadModel(
                    item.Id.Value,
                    item.ShoppingListId.Value,
                    item.ProductId?.Value,
                    item.Name,
                    item.Amount,
                    item.Unit?.ToString(),
                    item.Category,
                    item.Aisle,
                    item.Note,
                    item.IsChecked,
                    item.CheckedOnUtc,
                    item.SortOrder,
                    [.. item.Sources
                        .OrderBy(source => source.DayNumber ?? int.MaxValue)
                        .ThenBy(source => source.Label, StringComparer.Ordinal)
                        .Select(source => new ShoppingListItemSourceReadModel(
                            source.Id.Value,
                            source.SourceType.ToString(),
                            source.MealPlanId?.Value,
                            source.MealPlanMealId?.Value,
                            source.RecipeId?.Value,
                            source.Label,
                            source.DayNumber,
                            source.MealType,
                            source.Amount,
                            source.Unit?.ToString()))]))]);

    private static ShoppingListSummaryReadModel ToSummaryReadModel(ShoppingList list) =>
        new(list.Id.Value, list.Name, list.CreatedOnUtc, list.Items.Count);

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
    public async Task DeleteShoppingListCommandHandler_WithEmptyShoppingListId_ReturnsValidationFailure() {
        var handler = new DeleteShoppingListCommandHandler(new NoopShoppingListRepository(), CreateCurrentUserAccessService(User.Create("user@example.com", "hash")));
        Result result = await handler.Handle(new DeleteShoppingListCommand(Guid.NewGuid(), Guid.Empty), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ShoppingListId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteShoppingListCommandHandler_WithEmptyUserId_ReturnsInvalidToken() {
        var handler = new DeleteShoppingListCommandHandler(new NoopShoppingListRepository(), CreateCurrentUserAccessService(User.Create("user@example.com", "hash")));

        Result result = await handler.Handle(new DeleteShoppingListCommand(Guid.Empty, Guid.NewGuid()), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task DeleteShoppingListCommandHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-shopping-delete@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new DeleteShoppingListCommandHandler(new NoopShoppingListRepository(), CreateCurrentUserAccessService(user));

        Result result = await handler.Handle(new DeleteShoppingListCommand(user.Id.Value, Guid.NewGuid()), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task DeleteShoppingListCommandHandler_WhenListMissing_ReturnsNotFound() {
        var user = User.Create("shopping-delete-missing@example.com", "hash");
        var handler = new DeleteShoppingListCommandHandler(new NoopShoppingListRepository(), CreateCurrentUserAccessService(user));
        var shoppingListId = Guid.NewGuid();

        Result result = await handler.Handle(new DeleteShoppingListCommand(user.Id.Value, shoppingListId), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("ShoppingList.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task DeleteShoppingListCommandHandler_WithList_DeletesList() {
        var user = User.Create("shopping-delete-success@example.com", "hash");
        var list = ShoppingList.Create(user.Id, "Weekly");
        var repository = new SingleShoppingListRepository(list);
        var handler = new DeleteShoppingListCommandHandler(repository, CreateCurrentUserAccessService(user));

        Result result = await handler.Handle(new DeleteShoppingListCommand(user.Id.Value, list.Id.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.True(repository.DeleteCalled);
    }

    [Fact]
    public async Task UpdateShoppingListCommandHandler_WithEmptyShoppingListId_ReturnsValidationFailure() {
        var handler = new UpdateShoppingListCommandHandler(
            new NoopShoppingListRepository(),
            new NoopProductLookupService(),
            CreateCurrentUserAccessService(User.Create("user@example.com", "hash")));
        Result<ShoppingListModel> result = await handler.Handle(
            new UpdateShoppingListCommand(Guid.NewGuid(), Guid.Empty, "Weekly", Items: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ShoppingListId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateShoppingListCommandHandler_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new UpdateShoppingListCommandHandler(
            new NoopShoppingListRepository(),
            new NoopProductLookupService(),
            CreateCurrentUserAccessService(User.Create("user@example.com", "hash")));

        Result<ShoppingListModel> result = await handler.Handle(
            new UpdateShoppingListCommand(UserId: null, Guid.NewGuid(), "Weekly", Items: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task UpdateShoppingListCommandHandler_WithNothingToUpdate_ReturnsRequiredItems() {
        var handler = new UpdateShoppingListCommandHandler(
            new NoopShoppingListRepository(),
            new NoopProductLookupService(),
            CreateCurrentUserAccessService(User.Create("user@example.com", "hash")));

        Result<ShoppingListModel> result = await handler.Handle(
            new UpdateShoppingListCommand(Guid.NewGuid(), Guid.NewGuid(), Name: null, Items: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Required", result.Error.Code);
        Assert.Contains("Items", result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UpdateShoppingListCommandHandler_WhenListIsMissing_ReturnsNotFound() {
        var user = User.Create("shopping-list-missing@example.com", "hash");
        var handler = new UpdateShoppingListCommandHandler(
            new NoopShoppingListRepository(),
            new NoopProductLookupService(),
            CreateCurrentUserAccessService(user));

        var shoppingListId = Guid.NewGuid();
        Result<ShoppingListModel> result = await handler.Handle(
            new UpdateShoppingListCommand(user.Id.Value, shoppingListId, "Weekly", Items: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("ShoppingList.NotFound", result.Error.Code);
        Assert.Contains(shoppingListId.ToString(), result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateShoppingListCommandHandler_WithInaccessibleProduct_ReturnsProductNotAccessible() {
        var user = User.Create("shopping-owner@example.com", "hash");
        var list = ShoppingList.Create(user.Id, "Old");
        var handler = new UpdateShoppingListCommandHandler(
            new SingleShoppingListRepository(list),
            new NoopProductLookupService(),
            CreateCurrentUserAccessService(user));

        Result<ShoppingListModel> result = await handler.Handle(
            new UpdateShoppingListCommand(
                user.Id.Value,
                list.Id.Value,
                Name: null,
                [new ShoppingListItemInput(Id: null, ProductId: ProductId.New().Value, Name: null, Amount: 1, Unit: null, Category: null, Aisle: null, Note: null, IsChecked: false, CheckedOnUtc: null, SortOrder: null)]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Product.NotAccessible", result.Error.Code);
    }

    [Fact]
    public async Task UpdateShoppingListCommandHandler_WithNameAndItems_ReplacesItemsAndReturnsModel() {
        var user = User.Create("shopping-update@example.com", "hash");
        var product = Product.Create(
            user.Id,
            "Milk",
            MeasurementUnit.Ml,
            100,
            250,
            60,
            3,
            2,
            5,
            0,
            0,
            category: "Dairy");
        var list = ShoppingList.Create(user.Id, "Old");
        list.AddItem("Old item", productId: null, 1, unit: null, category: null, isChecked: false, 1);
        var repository = new SingleShoppingListRepository(list);
        var handler = new UpdateShoppingListCommandHandler(
            repository,
            new ProductLookupService(product),
            CreateCurrentUserAccessService(user));

        Result<ShoppingListModel> result = await handler.Handle(
            new UpdateShoppingListCommand(
                user.Id.Value,
                list.Id.Value,
                "  Weekly groceries  ",
                [
                    new ShoppingListItemInput(Id: null, ProductId: product.Id.Value, Name: null, Amount: 2, Unit: null, Category: null, Aisle: null, Note: null, IsChecked: true, CheckedOnUtc: null, SortOrder: null),
                    new ShoppingListItemInput(Id: null, ProductId: null, Name: "  Apples  ", Amount: 3, Unit: "Pcs", Category: "Fruit", Aisle: null, Note: null, IsChecked: false, CheckedOnUtc: null, SortOrder: 7),
                ]),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.True(repository.UpdateCalled);
        Assert.Equal("Weekly groceries", result.Value.Name);
        Assert.Equal(2, result.Value.Items.Count);
        Assert.Contains(result.Value.Items, item =>
            item.ProductId == product.Id.Value &&
            string.Equals(item.Name, "Milk", StringComparison.Ordinal) &&
            string.Equals(item.Unit, "Ml", StringComparison.Ordinal) &&
            string.Equals(item.Category, "Dairy", StringComparison.Ordinal) &&
            item.SortOrder == 1);
        Assert.Contains(result.Value.Items, item =>
            item.ProductId is null &&
            string.Equals(item.Name, "Apples", StringComparison.Ordinal) &&
            string.Equals(item.Unit, "Pcs", StringComparison.Ordinal) &&
            string.Equals(item.Category, "Fruit", StringComparison.Ordinal) &&
            item.SortOrder == 7);
    }

    [Fact]
    public async Task UpdateShoppingListCommandHandler_WithExistingItemId_UpdatesExistingItem() {
        var user = User.Create("shopping-update-existing-item@example.com", "hash");
        var list = ShoppingList.Create(user.Id, "Weekly");
        ShoppingListItem existing = list.AddItem(
            "Milk",
            productId: null,
            1,
            MeasurementUnit.Ml,
            "Dairy",
            isChecked: false,
            1);
        var checkedOnUtc = new DateTime(2030, 3, 28, 10, 0, 0, DateTimeKind.Utc);
        var repository = new SingleShoppingListRepository(list);
        var handler = new UpdateShoppingListCommandHandler(
            repository,
            new NoopProductLookupService(),
            CreateCurrentUserAccessService(user));

        Result<ShoppingListModel> result = await handler.Handle(
            new UpdateShoppingListCommand(
                user.Id.Value,
                list.Id.Value,
                Name: null,
                [
                    new ShoppingListItemInput(existing.Id.Value, ProductId: null, "  Eggs  ", Amount: 12, Unit: "Pcs", Category: "Protein", Aisle: "A1", Note: "organic", IsChecked: true, checkedOnUtc, SortOrder: 3),
                ]),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.True(repository.UpdateCalled);
        ShoppingListItemModel item = Assert.Single(result.Value.Items);
        Assert.Equal(existing.Id.Value, item.Id);
        Assert.Equal("Eggs", item.Name);
        Assert.Equal(12, item.Amount);
        Assert.Equal("Pcs", item.Unit);
        Assert.Equal("Protein", item.Category);
        Assert.Equal("A1", item.Aisle);
        Assert.Equal("organic", item.Note);
        Assert.True(item.IsChecked);
        Assert.Equal(checkedOnUtc, item.CheckedOnUtc);
        Assert.Equal(3, item.SortOrder);
    }

    [Fact]
    public async Task UpdateShoppingListCommandHandler_WithNameOnly_UpdatesWithoutReplacingItems() {
        var user = User.Create("shopping-update-name-only@example.com", "hash");
        var list = ShoppingList.Create(user.Id, "Old");
        list.AddItem("Milk", productId: null, 1, MeasurementUnit.Ml, "Dairy", isChecked: false, 1);
        var repository = new SingleShoppingListRepository(list);
        var handler = new UpdateShoppingListCommandHandler(
            repository,
            CreateThrowingProductLookupService(),
            CreateCurrentUserAccessService(user));

        Result<ShoppingListModel> result = await handler.Handle(
            new UpdateShoppingListCommand(user.Id.Value, list.Id.Value, "New", Items: null),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.True(repository.UpdateCalled);
        Assert.Equal("New", result.Value.Name);
        Assert.Single(result.Value.Items);
    }

    [Fact]
    public async Task CreateShoppingListCommandHandler_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new CreateShoppingListCommandHandler(
            new RecordingShoppingListRepository(),
            CreateThrowingProductLookupService(),
            CreateCurrentUserAccessService(User.Create("shopping-create@example.com", "hash")));

        Result<ShoppingListModel> result = await handler.Handle(
            new CreateShoppingListCommand(UserId: null, "Weekly", []),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task CreateShoppingListCommandHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-shopping-create@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new CreateShoppingListCommandHandler(
            new RecordingShoppingListRepository(),
            CreateThrowingProductLookupService(),
            CreateCurrentUserAccessService(user));

        Result<ShoppingListModel> result = await handler.Handle(new CreateShoppingListCommand(user.Id.Value, "Weekly", []), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task CreateShoppingListCommandHandler_WithEmptyName_ReturnsRequiredError() {
        var user = User.Create("shopping-empty-name@example.com", "hash");
        var handler = new CreateShoppingListCommandHandler(
            new RecordingShoppingListRepository(),
            CreateThrowingProductLookupService(),
            CreateCurrentUserAccessService(user));

        Result<ShoppingListModel> result = await handler.Handle(
            new CreateShoppingListCommand(user.Id.Value, " ", []),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Required", result.Error.Code);
    }

    [Fact]
    public async Task CreateShoppingListCommandHandler_WithInaccessibleProduct_ReturnsProductNotAccessible() {
        var user = User.Create("shopping-inaccessible-product@example.com", "hash");
        var handler = new CreateShoppingListCommandHandler(
            new RecordingShoppingListRepository(),
            new NoopProductLookupService(),
            CreateCurrentUserAccessService(user));

        Result<ShoppingListModel> result = await handler.Handle(
            new CreateShoppingListCommand(
                user.Id.Value,
                "Weekly",
                [new ShoppingListItemInput(Id: null, ProductId: ProductId.New().Value, Name: null, Amount: 1, Unit: null, Category: null, Aisle: null, Note: null, IsChecked: false, CheckedOnUtc: null, SortOrder: null)]),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Product.NotAccessible", result.Error.Code);
    }

    [Fact]
    public async Task CreateShoppingListCommandHandler_WithNameAndItems_AddsListAndReturnsModel() {
        var user = User.Create("shopping-create-ok@example.com", "hash");
        var product = Product.Create(
            user.Id,
            "Milk",
            MeasurementUnit.Ml,
            100,
            250,
            60,
            3,
            2,
            5,
            0,
            0,
            category: "Dairy");
        var repository = new RecordingShoppingListRepository();
        var handler = new CreateShoppingListCommandHandler(
            repository,
            new ProductLookupService(product),
            CreateCurrentUserAccessService(user));

        Result<ShoppingListModel> result = await handler.Handle(
            new CreateShoppingListCommand(
                user.Id.Value,
                "  Weekly  ",
                [
                    new ShoppingListItemInput(Id: null, ProductId: product.Id.Value, Name: null, Amount: 2, Unit: null, Category: null, Aisle: null, Note: null, IsChecked: true, CheckedOnUtc: null, SortOrder: null),
                    new ShoppingListItemInput(Id: null, ProductId: null, Name: "  Apples  ", Amount: 3, Unit: "Pcs", Category: "Fruit", Aisle: null, Note: null, IsChecked: false, CheckedOnUtc: null, SortOrder: 9),
                ]),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.NotNull(repository.AddedList);
        Assert.Equal("Weekly", result.Value.Name);
        Assert.Equal(2, result.Value.Items.Count);
        Assert.Contains(result.Value.Items, item =>
            item.ProductId == product.Id.Value &&
            string.Equals(item.Name, "Milk", StringComparison.Ordinal) &&
            item.IsChecked);
        Assert.Contains(result.Value.Items, item =>
            item.ProductId is null &&
            string.Equals(item.Name, "Apples", StringComparison.Ordinal) &&
            string.Equals(item.Unit, "Pcs", StringComparison.Ordinal) &&
            item.SortOrder == 9);
    }

    [Fact]
    public async Task ShoppingListItemBuilder_WithInvalidUnit_FailsWithUnitField() {
        ShoppingListItemInput[] items = [
            new ShoppingListItemInput(Id: null, ProductId: null, Name: "Milk", Amount: 1, Unit: "invalid_unit", Category: null, Aisle: null, Note: null, IsChecked: false, CheckedOnUtc: null, SortOrder: 1),
        ];

        Result<IReadOnlyList<ShoppingListItemData>> result = await ShoppingListItemBuilder.BuildItemsAsync(
            items,
            UserId.New(),
            new NoopProductLookupService(),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Contains("Unit", result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShoppingListItemBuilder_WithBlankUnit_CreatesCustomItemWithoutUnit() {
        Result<IReadOnlyList<ShoppingListItemData>> result = await ShoppingListItemBuilder.BuildItemsAsync(
            [new ShoppingListItemInput(Id: null, ProductId: null, Name: "Milk", Amount: 1, Unit: " ", Category: null, Aisle: null, Note: null, IsChecked: false, CheckedOnUtc: null, SortOrder: null)],
            UserId.New(),
            new NoopProductLookupService(),
            CancellationToken.None);

        ResultAssert.Success(result);
        ShoppingListItemData item = Assert.Single(result.Value);
        Assert.Null(item.Unit);
        Assert.Equal("Milk", item.Name);
    }

    [Fact]
    public async Task ShoppingListItemBuilder_WithNonPositiveAmount_Fails() {
        ShoppingListItemInput[] items = [
            new ShoppingListItemInput(Id: null, ProductId: null, Name: "Milk", Amount: 0, Unit: null, Category: null, Aisle: null, Note: null, IsChecked: false, CheckedOnUtc: null, SortOrder: 1),
        ];

        Result<IReadOnlyList<ShoppingListItemData>> result = await ShoppingListItemBuilder.BuildItemsAsync(
            items,
            UserId.New(),
            new NoopProductLookupService(),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public async Task ShoppingListItemBuilder_WithNonFiniteAmount_Fails(double amount) {
        ShoppingListItemInput[] items = [
            new ShoppingListItemInput(Id: null, ProductId: null, Name: "Milk", Amount: amount, Unit: null, Category: null, Aisle: null, Note: null, IsChecked: false, CheckedOnUtc: null, SortOrder: 1),
        ];

        Result<IReadOnlyList<ShoppingListItemData>> result = await ShoppingListItemBuilder.BuildItemsAsync(
            items,
            UserId.New(),
            new NoopProductLookupService(),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("finite", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ShoppingListItemBuilder_WithEmptyProductId_FailsWithValidationError() {
        ShoppingListItemInput[] items = [
            new ShoppingListItemInput(Id: null, ProductId: Guid.Empty, Name: null, Amount: 1, Unit: null, Category: null, Aisle: null, Note: null, IsChecked: false, CheckedOnUtc: null, SortOrder: 1),
        ];

        Result<IReadOnlyList<ShoppingListItemData>> result = await ShoppingListItemBuilder.BuildItemsAsync(
            items,
            UserId.New(),
            new NoopProductLookupService(),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ProductId", result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShoppingListItemBuilder_WithEmptyItemId_FailsWithValidationError() {
        ShoppingListItemInput[] items = [
            new ShoppingListItemInput(Id: Guid.Empty, ProductId: null, Name: "Milk", Amount: 1, Unit: null, Category: null, Aisle: null, Note: null, IsChecked: false, CheckedOnUtc: null, SortOrder: 1),
        ];

        Result<IReadOnlyList<ShoppingListItemData>> result = await ShoppingListItemBuilder.BuildItemsAsync(
            items,
            UserId.New(),
            new NoopProductLookupService(),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("Id", result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShoppingListItemBuilder_WithNoItems_ReturnsEmptyListWithoutProductLookup() {
        Result<IReadOnlyList<ShoppingListItemData>> result = await ShoppingListItemBuilder.BuildItemsAsync(
            [],
            UserId.New(),
            CreateThrowingProductLookupService(),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task ShoppingListItemBuilder_WithBlankCustomName_FailsWithNameRequired() {
        ShoppingListItemInput[] items = [
            new ShoppingListItemInput(Id: null, ProductId: null, Name: "   ", Amount: 1, Unit: null, Category: null, Aisle: null, Note: null, IsChecked: false, CheckedOnUtc: null, SortOrder: null),
        ];

        Result<IReadOnlyList<ShoppingListItemData>> result = await ShoppingListItemBuilder.BuildItemsAsync(
            items,
            UserId.New(),
            new NoopProductLookupService(),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Required", result.Error.Code);
        Assert.Contains("Name", result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShoppingListItemBuilder_WithProductCategoryOverride_UsesInputCategory() {
        var userId = UserId.New();
        var product = Product.Create(
            userId,
            "Milk",
            MeasurementUnit.Ml,
            100,
            250,
            60,
            3,
            2,
            5,
            0,
            0,
            category: "Dairy");

        Result<IReadOnlyList<ShoppingListItemData>> result = await ShoppingListItemBuilder.BuildItemsAsync(
            [
                new ShoppingListItemInput(Id: null, ProductId: product.Id.Value, Name: null, Amount: 1, Unit: null, Category: "Sale", Aisle: null, Note: null, IsChecked: false, CheckedOnUtc: null, SortOrder: 0),
            ],
            userId,
            new ProductLookupService(product),
            CancellationToken.None);

        ResultAssert.Success(result);
        ShoppingListItemData item = Assert.Single(result.Value);
        Assert.Equal("Sale", item.Category);
        Assert.Equal(1, item.SortOrder);
    }

    [Fact]
    public void ShoppingListMappings_ToSummaryModel_MapsItemCount() {
        var userId = UserId.New();
        var list = ShoppingList.Create(userId, "Weekly");
        list.AddItem("Milk", productId: null, 1, MeasurementUnit.Ml, "Dairy", isChecked: false, 1);
        list.AddItem("Apples", productId: null, 2, MeasurementUnit.Pcs, "Fruit", isChecked: true, 2);

        ShoppingListSummaryModel model = list.ToSummaryModel();

        Assert.Equal(list.Id.Value, model.Id);
        Assert.Equal("Weekly", model.Name);
        Assert.Equal(2, model.ItemsCount);
    }

    [ExcludeFromCodeCoverage]
    private sealed class NoopShoppingListRepository : IShoppingListRepository {
        public Task<ShoppingList> AddAsync(ShoppingList list, CancellationToken cancellationToken = default) => Task.FromResult(list);

        public Task<ShoppingList?> GetByIdAsync(
            ShoppingListId id,
            UserId userId,
            bool includeItems = false,
            bool asTracking = false,
            CancellationToken cancellationToken = default) => Task.FromResult<ShoppingList?>(null);

        public Task<ShoppingList?> GetCurrentAsync(
            UserId userId,
            bool includeItems = false,
            bool asTracking = false,
            CancellationToken cancellationToken = default) => Task.FromResult<ShoppingList?>(null);

        public Task<ShoppingListReadModel?> GetReadModelByIdAsync(ShoppingListId id, UserId userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<ShoppingListReadModel?>(null);

        public Task<ShoppingListReadModel?> GetCurrentReadModelAsync(UserId userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<ShoppingListReadModel?>(null);

        public Task<IReadOnlyList<ShoppingList>> GetAllAsync(
            UserId userId,
            bool includeItems = false,
            CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ShoppingList>>([]);

        public Task<IReadOnlyList<ShoppingListSummaryReadModel>> GetAllSummaryReadModelsAsync(UserId userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ShoppingListSummaryReadModel>>([]);

        public Task UpdateAsync(ShoppingList list, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(ShoppingList list, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingShoppingListRepository : IShoppingListRepository {
        public ShoppingList? AddedList { get; private set; }

        public Task<ShoppingList> AddAsync(ShoppingList list, CancellationToken cancellationToken = default) {
            AddedList = list;
            return Task.FromResult(list);
        }

        public Task<ShoppingList?> GetByIdAsync(
            ShoppingListId id,
            UserId userId,
            bool includeItems = false,
            bool asTracking = false,
            CancellationToken cancellationToken = default) => Task.FromResult<ShoppingList?>(null);

        public Task<ShoppingList?> GetCurrentAsync(
            UserId userId,
            bool includeItems = false,
            bool asTracking = false,
            CancellationToken cancellationToken = default) => Task.FromResult<ShoppingList?>(null);

        public Task<ShoppingListReadModel?> GetReadModelByIdAsync(ShoppingListId id, UserId userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<ShoppingListReadModel?>(null);

        public Task<ShoppingListReadModel?> GetCurrentReadModelAsync(UserId userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<ShoppingListReadModel?>(null);

        public Task<IReadOnlyList<ShoppingList>> GetAllAsync(
            UserId userId,
            bool includeItems = false,
            CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ShoppingList>>([]);

        public Task<IReadOnlyList<ShoppingListSummaryReadModel>> GetAllSummaryReadModelsAsync(UserId userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ShoppingListSummaryReadModel>>([]);

        public Task UpdateAsync(ShoppingList list, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(ShoppingList list, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    [ExcludeFromCodeCoverage]
    private sealed class SingleShoppingListRepository(ShoppingList list) : IShoppingListRepository {
        public bool UpdateCalled { get; private set; }
        public bool DeleteCalled { get; private set; }

        public Task<ShoppingList> AddAsync(ShoppingList addedList, CancellationToken cancellationToken = default) =>
            Task.FromResult(addedList);

        public Task<ShoppingList?> GetByIdAsync(
            ShoppingListId id,
            UserId userId,
            bool includeItems = false,
            bool asTracking = false,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<ShoppingList?>(id == list.Id && userId == list.UserId ? list : null);

        public Task<ShoppingList?> GetCurrentAsync(
            UserId userId,
            bool includeItems = false,
            bool asTracking = false,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<ShoppingList?>(userId == list.UserId ? list : null);

        public Task<ShoppingListReadModel?> GetReadModelByIdAsync(ShoppingListId id, UserId userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(id == list.Id && userId == list.UserId ? ToReadModel(list) : null);

        public Task<ShoppingListReadModel?> GetCurrentReadModelAsync(UserId userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(userId == list.UserId ? ToReadModel(list) : null);

        public Task<IReadOnlyList<ShoppingList>> GetAllAsync(
            UserId userId,
            bool includeItems = false,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ShoppingList>>(userId == list.UserId ? [list] : []);

        public Task<IReadOnlyList<ShoppingListSummaryReadModel>> GetAllSummaryReadModelsAsync(UserId userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ShoppingListSummaryReadModel>>(userId == list.UserId ? [ToSummaryReadModel(list)] : []);

        public Task UpdateAsync(ShoppingList updatedList, CancellationToken cancellationToken = default) {
            UpdateCalled = true;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(ShoppingList deletedList, CancellationToken cancellationToken = default) {
            DeleteCalled = true;
            return Task.CompletedTask;
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class NoopProductLookupService : IProductLookupService {
        public Task<IReadOnlyDictionary<ProductId, Product>> GetAccessibleByIdsAsync(
            IEnumerable<ProductId> ids,
            UserId userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<ProductId, Product>>(new Dictionary<ProductId, Product>());
    }

    private static IProductLookupService CreateThrowingProductLookupService() {
        IProductLookupService service = Substitute.For<IProductLookupService>();
        service
            .GetAccessibleByIdsAsync(Arg.Any<IEnumerable<ProductId>>(), Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns<Task<IReadOnlyDictionary<ProductId, Product>>>(_ => throw new InvalidOperationException("Product lookup should not be called."));

        return service;
    }

    [ExcludeFromCodeCoverage]
    private sealed class ProductLookupService(params Product[] products) : IProductLookupService {
        private readonly Dictionary<ProductId, Product> _products = products.ToDictionary(product => product.Id);

        public Task<IReadOnlyDictionary<ProductId, Product>> GetAccessibleByIdsAsync(
            IEnumerable<ProductId> ids,
            UserId userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<ProductId, Product>>(
                ids.Where(_products.ContainsKey).ToDictionary(id => id, id => _products[id]));
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

    private static ICurrentUserAccessService CreateCurrentUserAccessService(User user) {
        ICurrentUserAccessService service = Substitute.For<ICurrentUserAccessService>();
        service
            .EnsureCanAccessAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                UserId userId = call.Arg<UserId>();
                Error? error = user switch {
                    { Id: var id } when id != userId => Errors.Authentication.InvalidToken,
                    { DeletedAt: not null } => Errors.Authentication.AccountDeleted,
                    _ => null,
                };
                return Task.FromResult(error);
            });

        return service;
    }

    private static GetCurrentShoppingListQueryHandler CreateCurrentShoppingListHandler(
        IShoppingListReadModelRepository shoppingListRepository,
        ICurrentUserAccessService currentUserAccessService) =>
        new(new ShoppingListReadService(shoppingListRepository), currentUserAccessService);

    private static GetShoppingListByIdQueryHandler CreateShoppingListByIdHandler(
        IShoppingListReadModelRepository shoppingListRepository,
        ICurrentUserAccessService currentUserAccessService) =>
        new(new ShoppingListReadService(shoppingListRepository), currentUserAccessService);

    private static GetShoppingListsQueryHandler CreateShoppingListsHandler(
        IShoppingListReadModelRepository shoppingListRepository,
        ICurrentUserAccessService currentUserAccessService) =>
        new(new ShoppingListReadService(shoppingListRepository), currentUserAccessService);
}
