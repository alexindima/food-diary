using FoodDiary.Application.MealPlanning.ShoppingLists.Common;
using FoodDiary.Application.MealPlanning.ShoppingLists.Services;
using FoodDiary.Application.Abstractions.Products.Models;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;
using System.Reflection;

namespace FoodDiary.Application.Tests.ShoppingLists;

public partial class ShoppingListsFeatureTests {

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

    [Fact]
    public async Task ShoppingListItemBuilder_WithUnspecifiedCheckedTimestamp_ReturnsValidationFailure() {
        var unspecified = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Unspecified);
        ShoppingListItemInput[] items = [
            new ShoppingListItemInput(Id: null, ProductId: null, Name: "Milk", Amount: 1, Unit: null, Category: null, Aisle: null, Note: null, IsChecked: true, CheckedOnUtc: unspecified, SortOrder: 1),
        ];

        Result<IReadOnlyList<ShoppingListItemData>> result = await ShoppingListItemBuilder.BuildItemsAsync(
            items,
            UserId.New(),
            new NoopProductLookupService(),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Multiple(
            () => Assert.Equal("Validation.Invalid", result.Error.Code),
            () => Assert.Contains("CheckedOnUtc", result.Error.Message, StringComparison.Ordinal));
    }

    [Fact]
    public async Task ShoppingListItemBuilder_WithAmountOverDomainLimit_ReturnsValidationFailure() {
        Result<IReadOnlyList<ShoppingListItemData>> result = await ShoppingListItemBuilder.BuildItemsAsync(
            [new ShoppingListItemInput(Id: null, ProductId: null, Name: "Milk", Amount: 1_000_001d, Unit: null, Category: null, Aisle: null, Note: null, IsChecked: false, CheckedOnUtc: null, SortOrder: 1)],
            UserId.New(),
            new NoopProductLookupService(),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public async Task ShoppingListItemBuilder_WithTextOverDomainLimit_ReturnsValidationFailure() {
        Result<IReadOnlyList<ShoppingListItemData>> result = await ShoppingListItemBuilder.BuildItemsAsync(
            [new ShoppingListItemInput(Id: null, ProductId: null, Name: new string('x', 257), Amount: 1, Unit: null, Category: null, Aisle: null, Note: null, IsChecked: false, CheckedOnUtc: null, SortOrder: 1)],
            UserId.New(),
            new NoopProductLookupService(),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Theory]
    [InlineData("Category")]
    [InlineData("Aisle")]
    [InlineData("Note")]
    public async Task ShoppingListItemBuilder_WithOptionalTextOverDomainLimit_ReturnsValidationFailure(string field) {
        ShoppingListItemInput item = new(
            Id: null,
            ProductId: null,
            Name: "Milk",
            Amount: 1,
            Unit: null,
            Category: field.Equals("Category", StringComparison.Ordinal) ? new string('x', 129) : null,
            Aisle: field.Equals("Aisle", StringComparison.Ordinal) ? new string('x', 129) : null,
            Note: field.Equals("Note", StringComparison.Ordinal) ? new string('x', 513) : null,
            IsChecked: false,
            CheckedOnUtc: null,
            SortOrder: 1);

        Result<IReadOnlyList<ShoppingListItemData>> result = await ShoppingListItemBuilder.BuildItemsAsync(
            [item],
            UserId.New(),
            new NoopProductLookupService(),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public async Task ShoppingListItemBuilder_WithTooManyItems_ReturnsValidationFailureBeforeProductLookup() {
        ShoppingListItemInput item = new(Id: null, ProductId: null, Name: "Milk", Amount: 1, Unit: null, Category: null, Aisle: null, Note: null, IsChecked: false, CheckedOnUtc: null, SortOrder: 1);
        ShoppingListItemInput[] items = [.. Enumerable.Repeat(item, 501)];

        Result<IReadOnlyList<ShoppingListItemData>> result = await ShoppingListItemBuilder.BuildItemsAsync(
            items,
            UserId.New(),
            CreateThrowingProductLookupService(),
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
    public void ShoppingListItemBuilder_BuildProductItem_WithInvalidProductId_ReturnsValidationFailure() {
        MethodInfo method = typeof(ShoppingListItemBuilder).GetMethod(
            "BuildProductItem",
            BindingFlags.Static | BindingFlags.NonPublic)!;
        var input = new ShoppingListItemInput(
            Id: null,
            ProductId: Guid.Empty,
            Name: null,
            Amount: 1,
            Unit: null,
            Category: null,
            Aisle: null,
            Note: null,
            IsChecked: false,
            CheckedOnUtc: null,
            SortOrder: null);

        var result = (Result<ShoppingListItemData>)method.Invoke(
            obj: null,
            [input, 0, new Dictionary<ProductId, ProductOverviewReadItem>()])!;

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ProductId", result.Error.Message, StringComparison.Ordinal);
    }
}
