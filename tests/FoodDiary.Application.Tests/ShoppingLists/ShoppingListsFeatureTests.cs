using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.ShoppingLists.Commands.Common;
using FoodDiary.Application.ShoppingLists.Commands.CreateShoppingList;
using FoodDiary.Application.ShoppingLists.Commands.DeleteShoppingList;
using FoodDiary.Application.ShoppingLists.Commands.UpdateShoppingList;
using FoodDiary.Application.Abstractions.ShoppingLists.Common;
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
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.ShoppingLists.Models;

namespace FoodDiary.Application.Tests.ShoppingLists;

[ExcludeFromCodeCoverage]
public class ShoppingListsFeatureTests {
    [Fact]
    public async Task GetCurrentShoppingListQueryHandler_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new GetCurrentShoppingListQueryHandler(new NoopShoppingListRepository(), new StubUserRepository(User.Create("user@example.com", "hash")));
        Result<ShoppingListModel> result = await handler.Handle(new GetCurrentShoppingListQuery(UserId: null), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetCurrentShoppingListQueryHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-current-shopping@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new GetCurrentShoppingListQueryHandler(new NoopShoppingListRepository(), new StubUserRepository(user));

        Result<ShoppingListModel> result = await handler.Handle(new GetCurrentShoppingListQuery(user.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task GetShoppingListByIdQueryHandler_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new GetShoppingListByIdQueryHandler(new NoopShoppingListRepository(), new StubUserRepository(User.Create("user@example.com", "hash")));
        Result<ShoppingListModel> result = await handler.Handle(new GetShoppingListByIdQuery(UserId: null, Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetShoppingListByIdQueryHandler_WithEmptyShoppingListId_ReturnsValidationFailure() {
        var handler = new GetShoppingListByIdQueryHandler(new NoopShoppingListRepository(), new StubUserRepository(User.Create("user@example.com", "hash")));
        Result<ShoppingListModel> result = await handler.Handle(new GetShoppingListByIdQuery(Guid.NewGuid(), Guid.Empty), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ShoppingListId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetShoppingListByIdQueryHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-shopping-by-id@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new GetShoppingListByIdQueryHandler(new NoopShoppingListRepository(), new StubUserRepository(user));

        Result<ShoppingListModel> result = await handler.Handle(new GetShoppingListByIdQuery(user.Id.Value, Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task GetShoppingListByIdQueryHandler_WhenListMissing_ReturnsNotFound() {
        var user = User.Create("shopping-by-id-missing@example.com", "hash");
        var handler = new GetShoppingListByIdQueryHandler(new NoopShoppingListRepository(), new StubUserRepository(user));
        var shoppingListId = Guid.NewGuid();

        Result<ShoppingListModel> result = await handler.Handle(new GetShoppingListByIdQuery(user.Id.Value, shoppingListId), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("ShoppingList.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task GetShoppingListByIdQueryHandler_WithList_ReturnsModel() {
        var user = User.Create("shopping-by-id-success@example.com", "hash");
        var list = ShoppingList.Create(user.Id, "Weekly");
        list.AddItem("Milk", productId: null, 1, MeasurementUnit.Ml, "Dairy", isChecked: false, 1);
        var handler = new GetShoppingListByIdQueryHandler(new SingleShoppingListRepository(list), new StubUserRepository(user));

        Result<ShoppingListModel> result = await handler.Handle(new GetShoppingListByIdQuery(user.Id.Value, list.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Weekly", result.Value.Name);
        Assert.Single(result.Value.Items);
    }

    [Fact]
    public async Task GetShoppingListsQueryHandler_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new GetShoppingListsQueryHandler(new NoopShoppingListRepository(), new StubUserRepository(User.Create("user@example.com", "hash")));
        Result<IReadOnlyList<ShoppingListSummaryModel>> result = await handler.Handle(new GetShoppingListsQuery(UserId: null), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetShoppingListsQueryHandler_WithLists_ReturnsSummaryModels() {
        var user = User.Create("shopping-lists-success@example.com", "hash");
        var list = ShoppingList.Create(user.Id, "Weekly");
        list.AddItem("Milk", productId: null, 1, MeasurementUnit.Ml, "Dairy", isChecked: false, 1);
        var handler = new GetShoppingListsQueryHandler(new SingleShoppingListRepository(list), new StubUserRepository(user));

        Result<IReadOnlyList<ShoppingListSummaryModel>> result = await handler.Handle(new GetShoppingListsQuery(user.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        ShoppingListSummaryModel summary = Assert.Single(result.Value);
        Assert.Equal(list.Id.Value, summary.Id);
        Assert.Equal("Weekly", summary.Name);
        Assert.Equal(1, summary.ItemsCount);
    }

    [Fact]
    public async Task DeleteShoppingListCommandHandler_WithEmptyShoppingListId_ReturnsValidationFailure() {
        var handler = new DeleteShoppingListCommandHandler(new NoopShoppingListRepository(), new StubUserRepository(User.Create("user@example.com", "hash")));
        Result result = await handler.Handle(new DeleteShoppingListCommand(Guid.NewGuid(), Guid.Empty), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ShoppingListId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteShoppingListCommandHandler_WithEmptyUserId_ReturnsInvalidToken() {
        var handler = new DeleteShoppingListCommandHandler(new NoopShoppingListRepository(), new StubUserRepository(User.Create("user@example.com", "hash")));

        Result result = await handler.Handle(new DeleteShoppingListCommand(Guid.Empty, Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task DeleteShoppingListCommandHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-shopping-delete@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new DeleteShoppingListCommandHandler(new NoopShoppingListRepository(), new StubUserRepository(user));

        Result result = await handler.Handle(new DeleteShoppingListCommand(user.Id.Value, Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task DeleteShoppingListCommandHandler_WhenListMissing_ReturnsNotFound() {
        var user = User.Create("shopping-delete-missing@example.com", "hash");
        var handler = new DeleteShoppingListCommandHandler(new NoopShoppingListRepository(), new StubUserRepository(user));
        var shoppingListId = Guid.NewGuid();

        Result result = await handler.Handle(new DeleteShoppingListCommand(user.Id.Value, shoppingListId), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("ShoppingList.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task DeleteShoppingListCommandHandler_WithList_DeletesList() {
        var user = User.Create("shopping-delete-success@example.com", "hash");
        var list = ShoppingList.Create(user.Id, "Weekly");
        var repository = new SingleShoppingListRepository(list);
        var handler = new DeleteShoppingListCommandHandler(repository, new StubUserRepository(user));

        Result result = await handler.Handle(new DeleteShoppingListCommand(user.Id.Value, list.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(repository.DeleteCalled);
    }

    [Fact]
    public async Task UpdateShoppingListCommandHandler_WithEmptyShoppingListId_ReturnsValidationFailure() {
        var handler = new UpdateShoppingListCommandHandler(
            new NoopShoppingListRepository(),
            new NoopProductLookupService(),
            new StubUserRepository(User.Create("user@example.com", "hash")));
        Result<ShoppingListModel> result = await handler.Handle(
            new UpdateShoppingListCommand(Guid.NewGuid(), Guid.Empty, "Weekly", Items: null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ShoppingListId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateShoppingListCommandHandler_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new UpdateShoppingListCommandHandler(
            new NoopShoppingListRepository(),
            new NoopProductLookupService(),
            new StubUserRepository(User.Create("user@example.com", "hash")));

        Result<ShoppingListModel> result = await handler.Handle(
            new UpdateShoppingListCommand(UserId: null, Guid.NewGuid(), "Weekly", Items: null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task UpdateShoppingListCommandHandler_WithNothingToUpdate_ReturnsRequiredItems() {
        var handler = new UpdateShoppingListCommandHandler(
            new NoopShoppingListRepository(),
            new NoopProductLookupService(),
            new StubUserRepository(User.Create("user@example.com", "hash")));

        Result<ShoppingListModel> result = await handler.Handle(
            new UpdateShoppingListCommand(Guid.NewGuid(), Guid.NewGuid(), Name: null, Items: null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Required", result.Error.Code);
        Assert.Contains("Items", result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UpdateShoppingListCommandHandler_WhenListIsMissing_ReturnsNotFound() {
        var handler = new UpdateShoppingListCommandHandler(
            new NoopShoppingListRepository(),
            new NoopProductLookupService(),
            new StubUserRepository(User.Create("user@example.com", "hash")));

        var shoppingListId = Guid.NewGuid();
        Result<ShoppingListModel> result = await handler.Handle(
            new UpdateShoppingListCommand(Guid.NewGuid(), shoppingListId, "Weekly", Items: null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
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
            new StubUserRepository(user));

        Result<ShoppingListModel> result = await handler.Handle(
            new UpdateShoppingListCommand(
                user.Id.Value,
                list.Id.Value,
                Name: null,
                [new ShoppingListItemInput(ProductId.New().Value, Name: null, 1, Unit: null, Category: null, IsChecked: false, SortOrder: null)]),
            CancellationToken.None);

        Assert.True(result.IsFailure);
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
            new StubUserRepository(user));

        Result<ShoppingListModel> result = await handler.Handle(
            new UpdateShoppingListCommand(
                user.Id.Value,
                list.Id.Value,
                "  Weekly groceries  ",
                [
                    new ShoppingListItemInput(product.Id.Value, Name: null, 2, Unit: null, Category: null, IsChecked: true, SortOrder: null),
                    new ShoppingListItemInput(ProductId: null, "  Apples  ", 3, "Pcs", "Fruit", IsChecked: false, 7),
                ]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
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
    public async Task UpdateShoppingListCommandHandler_WithNameOnly_UpdatesWithoutReplacingItems() {
        var user = User.Create("shopping-update-name-only@example.com", "hash");
        var list = ShoppingList.Create(user.Id, "Old");
        list.AddItem("Milk", productId: null, 1, MeasurementUnit.Ml, "Dairy", isChecked: false, 1);
        var repository = new SingleShoppingListRepository(list);
        var handler = new UpdateShoppingListCommandHandler(
            repository,
            new ThrowingProductLookupService(),
            new StubUserRepository(user));

        Result<ShoppingListModel> result = await handler.Handle(
            new UpdateShoppingListCommand(user.Id.Value, list.Id.Value, "New", Items: null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(repository.UpdateCalled);
        Assert.Equal("New", result.Value.Name);
        Assert.Single(result.Value.Items);
    }

    [Fact]
    public async Task CreateShoppingListCommandHandler_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new CreateShoppingListCommandHandler(
            new RecordingShoppingListRepository(),
            new ThrowingProductLookupService(),
            new StubUserRepository(User.Create("shopping-create@example.com", "hash")));

        Result<ShoppingListModel> result = await handler.Handle(
            new CreateShoppingListCommand(UserId: null, "Weekly", []),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task CreateShoppingListCommandHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-shopping-create@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new CreateShoppingListCommandHandler(
            new RecordingShoppingListRepository(),
            new ThrowingProductLookupService(),
            new StubUserRepository(user));

        Result<ShoppingListModel> result = await handler.Handle(new CreateShoppingListCommand(user.Id.Value, "Weekly", []), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task CreateShoppingListCommandHandler_WithEmptyName_ReturnsRequiredError() {
        var user = User.Create("shopping-empty-name@example.com", "hash");
        var handler = new CreateShoppingListCommandHandler(
            new RecordingShoppingListRepository(),
            new ThrowingProductLookupService(),
            new StubUserRepository(user));

        Result<ShoppingListModel> result = await handler.Handle(
            new CreateShoppingListCommand(user.Id.Value, " ", []),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Required", result.Error.Code);
    }

    [Fact]
    public async Task CreateShoppingListCommandHandler_WithInaccessibleProduct_ReturnsProductNotAccessible() {
        var user = User.Create("shopping-inaccessible-product@example.com", "hash");
        var handler = new CreateShoppingListCommandHandler(
            new RecordingShoppingListRepository(),
            new NoopProductLookupService(),
            new StubUserRepository(user));

        Result<ShoppingListModel> result = await handler.Handle(
            new CreateShoppingListCommand(
                user.Id.Value,
                "Weekly",
                [new ShoppingListItemInput(ProductId.New().Value, Name: null, 1, Unit: null, Category: null, IsChecked: false, SortOrder: null)]),
            CancellationToken.None);

        Assert.True(result.IsFailure);
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
            new StubUserRepository(user));

        Result<ShoppingListModel> result = await handler.Handle(
            new CreateShoppingListCommand(
                user.Id.Value,
                "  Weekly  ",
                [
                    new ShoppingListItemInput(product.Id.Value, Name: null, 2, Unit: null, Category: null, IsChecked: true, SortOrder: null),
                    new ShoppingListItemInput(ProductId: null, "  Apples  ", 3, "Pcs", "Fruit", IsChecked: false, 9),
                ]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
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
            new ShoppingListItemInput(ProductId: null, "Milk", 1, "invalid_unit", Category: null, IsChecked: false, 1),
        ];

        Result<IReadOnlyList<ShoppingListItemData>> result = await ShoppingListItemBuilder.BuildItemsAsync(
            items,
            UserId.New(),
            new NoopProductLookupService(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("Unit", result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShoppingListItemBuilder_WithBlankUnit_CreatesCustomItemWithoutUnit() {
        Result<IReadOnlyList<ShoppingListItemData>> result = await ShoppingListItemBuilder.BuildItemsAsync(
            [new ShoppingListItemInput(ProductId: null, "Milk", 1, " ", Category: null, IsChecked: false, SortOrder: null)],
            UserId.New(),
            new NoopProductLookupService(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        ShoppingListItemData item = Assert.Single(result.Value);
        Assert.Null(item.Unit);
        Assert.Equal("Milk", item.Name);
    }

    [Fact]
    public async Task ShoppingListItemBuilder_WithNonPositiveAmount_Fails() {
        ShoppingListItemInput[] items = [
            new ShoppingListItemInput(ProductId: null, "Milk", 0, Unit: null, Category: null, IsChecked: false, 1),
        ];

        Result<IReadOnlyList<ShoppingListItemData>> result = await ShoppingListItemBuilder.BuildItemsAsync(
            items,
            UserId.New(),
            new NoopProductLookupService(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public async Task ShoppingListItemBuilder_WithNonFiniteAmount_Fails(double amount) {
        ShoppingListItemInput[] items = [
            new ShoppingListItemInput(ProductId: null, "Milk", amount, Unit: null, Category: null, IsChecked: false, 1),
        ];

        Result<IReadOnlyList<ShoppingListItemData>> result = await ShoppingListItemBuilder.BuildItemsAsync(
            items,
            UserId.New(),
            new NoopProductLookupService(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("finite", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ShoppingListItemBuilder_WithEmptyProductId_FailsWithValidationError() {
        ShoppingListItemInput[] items = [
            new ShoppingListItemInput(Guid.Empty, Name: null, 1, Unit: null, Category: null, IsChecked: false, 1),
        ];

        Result<IReadOnlyList<ShoppingListItemData>> result = await ShoppingListItemBuilder.BuildItemsAsync(
            items,
            UserId.New(),
            new NoopProductLookupService(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ProductId", result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShoppingListItemBuilder_WithNoItems_ReturnsEmptyListWithoutProductLookup() {
        Result<IReadOnlyList<ShoppingListItemData>> result = await ShoppingListItemBuilder.BuildItemsAsync(
            [],
            UserId.New(),
            new ThrowingProductLookupService(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task ShoppingListItemBuilder_WithBlankCustomName_FailsWithNameRequired() {
        ShoppingListItemInput[] items = [
            new ShoppingListItemInput(ProductId: null, "   ", 1, Unit: null, Category: null, IsChecked: false, SortOrder: null),
        ];

        Result<IReadOnlyList<ShoppingListItemData>> result = await ShoppingListItemBuilder.BuildItemsAsync(
            items,
            UserId.New(),
            new NoopProductLookupService(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
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
                new ShoppingListItemInput(product.Id.Value, Name: null, 1, Unit: null, "Sale", IsChecked: false, 0),
            ],
            userId,
            new ProductLookupService(product),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
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

        public Task<IReadOnlyList<ShoppingList>> GetAllAsync(
            UserId userId,
            bool includeItems = false,
            CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ShoppingList>>([]);

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

        public Task<IReadOnlyList<ShoppingList>> GetAllAsync(
            UserId userId,
            bool includeItems = false,
            CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ShoppingList>>([]);

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

        public Task<IReadOnlyList<ShoppingList>> GetAllAsync(
            UserId userId,
            bool includeItems = false,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ShoppingList>>(userId == list.UserId ? [list] : []);

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

    [ExcludeFromCodeCoverage]
    private sealed class ThrowingProductLookupService : IProductLookupService {
        public Task<IReadOnlyDictionary<ProductId, Product>> GetAccessibleByIdsAsync(
            IEnumerable<ProductId> ids,
            UserId userId,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Product lookup should not be called.");
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
        var handler = new GetShoppingListsQueryHandler(new NoopShoppingListRepository(), new StubUserRepository(user));

        Result<IReadOnlyList<ShoppingListSummaryModel>> result = await handler.Handle(new GetShoppingListsQuery(user.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubUserRepository(User user) : IUserRepository {
        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default) => Task.FromResult<User?>(user);
        public Task<User?> GetByIdIncludingDeletedAsync(UserId id, CancellationToken cancellationToken = default) => Task.FromResult<User?>(user);
        public Task<User?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByTelegramUserIdIncludingDeletedAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(string? search, int page, int limit, bool includeDeleted, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers)> GetAdminDashboardSummaryAsync(int recentLimit, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Role>> GetRolesByNamesAsync(IReadOnlyList<string> names, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User> AddAsync(User addedUser, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(User updatedUser, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
