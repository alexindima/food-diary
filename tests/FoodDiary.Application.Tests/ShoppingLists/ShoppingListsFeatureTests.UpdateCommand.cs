using FoodDiary.Application.ShoppingLists.Common;
using FoodDiary.Application.ShoppingLists.Commands.UpdateShoppingList;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Shopping;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;
using FoodDiary.Application.ShoppingLists.Models;

namespace FoodDiary.Application.Tests.ShoppingLists;

public partial class ShoppingListsFeatureTests {

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
}
