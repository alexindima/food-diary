using FoodDiary.Application.ShoppingLists.Common;
using FoodDiary.Application.ShoppingLists.Commands.CreateShoppingList;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;
using FoodDiary.Application.ShoppingLists.Models;

namespace FoodDiary.Application.Tests.ShoppingLists;

public partial class ShoppingListsFeatureTests {

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
}
