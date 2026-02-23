using FoodDiary.Domain.Entities.Shopping;
using FoodDiary.Domain.Events;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Domain;

public class ShoppingListInvariantTests {
    [Fact]
    public void Create_WithBlankName_Throws() {
        Assert.Throws<ArgumentException>(() => ShoppingList.Create(UserId.New(), "   "));
    }

    [Fact]
    public void AddItem_WithNegativeSortOrder_Throws() {
        var list = ShoppingList.Create(UserId.New(), "Weekly");

        Assert.Throws<ArgumentOutOfRangeException>(() => list.AddItem(
            name: "Milk",
            productId: null,
            amount: 1,
            unit: null,
            category: null,
            isChecked: false,
            sortOrder: -1));
    }

    [Fact]
    public void Create_WithEmptyUserId_Throws() {
        Assert.Throws<ArgumentException>(() => ShoppingList.Create(UserId.Empty, "Weekly"));
    }

    [Fact]
    public void UpdateName_WithSameTrimmedValue_DoesNotSetModifiedOnUtc() {
        var list = ShoppingList.Create(UserId.New(), "Weekly");

        list.UpdateName("  Weekly  ");

        Assert.Null(list.ModifiedOnUtc);
    }

    [Fact]
    public void ClearItems_WhenAlreadyEmpty_DoesNotSetModifiedOnUtc() {
        var list = ShoppingList.Create(UserId.New(), "Weekly");

        list.ClearItems();

        Assert.Null(list.ModifiedOnUtc);
    }

    [Fact]
    public void AddItem_WithEmptyProductId_Throws() {
        var list = ShoppingList.Create(UserId.New(), "Weekly");

        Assert.Throws<ArgumentException>(() => list.AddItem(
            name: "Milk",
            productId: ProductId.Empty,
            amount: 1,
            unit: null,
            category: null,
            isChecked: false,
            sortOrder: 0));
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(-1d)]
    [InlineData(1000000.0001d)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NaN)]
    public void AddItem_WithInvalidAmount_Throws(double amount) {
        var list = ShoppingList.Create(UserId.New(), "Weekly");

        Assert.Throws<ArgumentOutOfRangeException>(() => list.AddItem(
            name: "Milk",
            productId: null,
            amount: amount,
            unit: null,
            category: null,
            isChecked: false,
            sortOrder: 0));
    }

    [Fact]
    public void AddItem_WithWhitespaceCategory_NormalizesToNull() {
        var list = ShoppingList.Create(UserId.New(), "Weekly");

        var item = list.AddItem(
            name: "Milk",
            productId: null,
            amount: 1,
            unit: null,
            category: "   ",
            isChecked: false,
            sortOrder: 0);

        Assert.Null(item.Category);
    }

    [Fact]
    public void UpdateName_WithDifferentValue_RaisesNameUpdatedDomainEvent() {
        var list = ShoppingList.Create(UserId.New(), "Weekly");

        list.UpdateName("Monthly");

        var evt = Assert.Single(list.DomainEvents.OfType<ShoppingListNameUpdatedDomainEvent>());
        Assert.Equal(list.Id, evt.ShoppingListId);
        Assert.Equal("Weekly", evt.PreviousName);
        Assert.Equal("Monthly", evt.CurrentName);
    }

    [Fact]
    public void AddItem_RaisesItemAddedDomainEvent() {
        var list = ShoppingList.Create(UserId.New(), "Weekly");

        var item = list.AddItem(
            name: "Milk",
            productId: null,
            amount: 1,
            unit: null,
            category: "Dairy",
            isChecked: false,
            sortOrder: 1);

        var evt = Assert.Single(list.DomainEvents.OfType<ShoppingListItemAddedDomainEvent>());
        Assert.Equal(list.Id, evt.ShoppingListId);
        Assert.Equal(item.Id, evt.ShoppingListItemId);
        Assert.Equal("Milk", evt.Name);
        Assert.Equal("Dairy", evt.Category);
        Assert.Equal(1, evt.SortOrder);
    }

    [Fact]
    public void ClearItems_WhenHasItems_RaisesItemsClearedDomainEvent() {
        var list = ShoppingList.Create(UserId.New(), "Weekly");
        list.AddItem(
            name: "Milk",
            productId: null,
            amount: 1,
            unit: null,
            category: null,
            isChecked: false,
            sortOrder: 0);
        list.AddItem(
            name: "Eggs",
            productId: null,
            amount: 10,
            unit: null,
            category: null,
            isChecked: false,
            sortOrder: 1);
        list.ClearDomainEvents();

        list.ClearItems();

        var evt = Assert.Single(list.DomainEvents.OfType<ShoppingListItemsClearedDomainEvent>());
        Assert.Equal(list.Id, evt.ShoppingListId);
        Assert.Equal(2, evt.ClearedItemsCount);
    }

    [Fact]
    public void UpdateName_WithSameTrimmedValue_DoesNotRaiseDomainEvent() {
        var list = ShoppingList.Create(UserId.New(), "Weekly");

        list.UpdateName("  Weekly  ");

        Assert.DoesNotContain(list.DomainEvents, e => e is ShoppingListNameUpdatedDomainEvent);
    }

    [Fact]
    public void ClearItems_WhenAlreadyEmpty_DoesNotRaiseDomainEvent() {
        var list = ShoppingList.Create(UserId.New(), "Weekly");

        list.ClearItems();

        Assert.DoesNotContain(list.DomainEvents, e => e is ShoppingListItemsClearedDomainEvent);
    }
}
