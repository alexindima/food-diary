using FoodDiary.Domain.Entities.Shopping;
using FoodDiary.Domain.Events;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
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
    public void AddItem_WithTooLongName_Throws() {
        var list = ShoppingList.Create(UserId.New(), "Weekly");

        Assert.Throws<ArgumentOutOfRangeException>(() => list.AddItem(
            name: new string('m', 257),
            productId: null,
            amount: 1,
            unit: null,
            category: null,
            isChecked: false,
            sortOrder: 0));
    }

    [Fact]
    public void AddItem_WithBlankName_Throws() {
        var list = ShoppingList.Create(UserId.New(), "Weekly");

        Assert.Throws<ArgumentException>(() => list.AddItem(
            name: "   ",
            productId: null,
            amount: 1,
            unit: null,
            category: null,
            isChecked: false,
            sortOrder: 0));
    }

    [Fact]
    public void ShoppingListItem_Create_WithEmptyShoppingListId_Throws() {
        Assert.Throws<ArgumentException>(() => ShoppingListItem.Create(
            ShoppingListId.Empty,
            name: "Milk",
            productId: null,
            amount: 1,
            unit: null,
            category: null,
            isChecked: false,
            sortOrder: 0));
    }

    [Fact]
    public void ShoppingListItem_Create_WithEmptyItemId_Throws() {
        Assert.Throws<ArgumentException>(() => ShoppingListItem.Create(
            ShoppingListId.New(),
            name: "Milk",
            productId: null,
            amount: 1,
            unit: null,
            category: null,
            isChecked: false,
            sortOrder: 0,
            id: ShoppingListItemId.Empty));
    }

    [Fact]
    public void AddItem_WithTooLongCategory_Throws() {
        var list = ShoppingList.Create(UserId.New(), "Weekly");

        Assert.Throws<ArgumentOutOfRangeException>(() => list.AddItem(
            name: "Milk",
            productId: null,
            amount: 1,
            unit: null,
            category: new string('c', 129),
            isChecked: false,
            sortOrder: 0));
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

        ShoppingListItem item = list.AddItem(
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
    public void AddItem_WithNullAmountAndTrimmedFields_AddsItem() {
        var list = ShoppingList.Create(UserId.New(), "Weekly");

        ShoppingListItem item = list.AddItem(
            name: "  Milk  ",
            productId: null,
            amount: null,
            unit: MeasurementUnit.Ml,
            category: "  Dairy  ",
            isChecked: true,
            sortOrder: 0);

        Assert.Multiple(
            () => Assert.Equal("Milk", item.Name),
            () => Assert.Null(item.Amount),
            () => Assert.Equal(MeasurementUnit.Ml, item.Unit),
            () => Assert.Equal("Dairy", item.Category),
            () => Assert.True(item.IsChecked));
        Assert.NotNull(list.ModifiedOnUtc);
    }

    [Fact]
    public void AddItem_WithMaximumAmount_AddsItem() {
        var list = ShoppingList.Create(UserId.New(), "Weekly");

        ShoppingListItem item = list.AddItem(
            name: "Rice",
            productId: null,
            amount: 1000000d,
            unit: MeasurementUnit.G,
            category: null,
            isChecked: false,
            sortOrder: 0);

        Assert.Equal(1000000d, item.Amount);
    }

    [Fact]
    public void FindItem_WhenItemExists_ReturnsItem() {
        var list = ShoppingList.Create(UserId.New(), "Weekly");
        ShoppingListItem item = list.AddItem(
            name: "Milk",
            productId: null,
            amount: 1,
            unit: null,
            category: null,
            isChecked: false,
            sortOrder: 0);

        ShoppingListItem? found = list.FindItem(item.Id);

        Assert.Same(item, found);
    }

    [Fact]
    public void FindItem_WhenItemDoesNotExist_ReturnsNull() {
        var list = ShoppingList.Create(UserId.New(), "Weekly");
        list.AddItem(
            name: "Milk",
            productId: null,
            amount: 1,
            unit: null,
            category: null,
            isChecked: false,
            sortOrder: 0);

        ShoppingListItem? found = list.FindItem(ShoppingListItemId.New());

        Assert.Null(found);
    }

    [Fact]
    public void RemoveItemsExcept_WhenNothingRemoved_DoesNotSetModifiedOnUtc() {
        var list = ShoppingList.Create(UserId.New(), "Weekly");
        ShoppingListItem item = list.AddItem(
            name: "Milk",
            productId: null,
            amount: 1,
            unit: null,
            category: null,
            isChecked: false,
            sortOrder: 0);
        DateTime? modifiedOnUtc = list.ModifiedOnUtc;

        list.RemoveItemsExcept(new HashSet<ShoppingListItemId> { item.Id });

        Assert.Equal(modifiedOnUtc, list.ModifiedOnUtc);
        Assert.Same(item, Assert.Single(list.Items));
    }

    [Fact]
    public void RemoveItemsExcept_WhenItemsAreRemoved_RemovesOnlyExcludedItems() {
        var list = ShoppingList.Create(UserId.New(), "Weekly");
        ShoppingListItem kept = list.AddItem(
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
            amount: 12,
            unit: null,
            category: null,
            isChecked: false,
            sortOrder: 1);

        list.RemoveItemsExcept(new HashSet<ShoppingListItemId> { kept.Id });

        Assert.Same(kept, Assert.Single(list.Items));
    }

    [Fact]
    public void ShoppingListItem_Create_WithExplicitItemId_UsesProvidedId() {
        var id = ShoppingListItemId.New();

        var item = ShoppingListItem.Create(
            ShoppingListId.New(),
            name: "Milk",
            productId: null,
            amount: 1,
            unit: null,
            category: null,
            isChecked: false,
            sortOrder: 0,
            id: id);

        Assert.Equal(id, item.Id);
    }

    [Fact]
    public void ShoppingListItem_UpdateDetails_UpdatesFieldsAndModifiedTimestamp() {
        var list = ShoppingList.Create(UserId.New(), "Weekly");
        ShoppingListItem item = list.AddItem(
            name: "Milk",
            productId: null,
            amount: 1,
            unit: MeasurementUnit.Ml,
            category: "Dairy",
            isChecked: false,
            sortOrder: 0);
        var checkedAt = new DateTime(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc);
        var productId = ProductId.New();

        item.UpdateDetails(
            name: "  Eggs  ",
            productId,
            amount: 12,
            unit: MeasurementUnit.Pcs,
            category: "  Protein  ",
            aisle: "  A1  ",
            note: "  organic  ",
            isChecked: true,
            checkedOnUtc: checkedAt,
            sortOrder: 2);

        Assert.Multiple(
            () => Assert.Equal(productId, item.ProductId),
            () => Assert.Equal("Eggs", item.Name),
            () => Assert.Equal(12, item.Amount),
            () => Assert.Equal(MeasurementUnit.Pcs, item.Unit),
            () => Assert.Equal("Protein", item.Category),
            () => Assert.Equal("A1", item.Aisle),
            () => Assert.Equal("organic", item.Note),
            () => Assert.True(item.IsChecked),
            () => Assert.Equal(checkedAt, item.CheckedOnUtc),
            () => Assert.Equal(2, item.SortOrder));
        Assert.NotNull(item.ModifiedOnUtc);
    }

    [Fact]
    public void ShoppingListItem_UpdateDetails_WhenUnchecked_ClearsCheckedTimestamp() {
        var list = ShoppingList.Create(UserId.New(), "Weekly");
        ShoppingListItem item = list.AddItem(
            name: "Milk",
            productId: null,
            amount: 1,
            unit: MeasurementUnit.Ml,
            category: null,
            isChecked: true,
            sortOrder: 0,
            checkedOnUtc: DateTime.UtcNow);

        item.UpdateDetails(
            name: "Milk",
            productId: null,
            amount: 1,
            unit: MeasurementUnit.Ml,
            category: null,
            aisle: null,
            note: null,
            isChecked: false,
            checkedOnUtc: DateTime.UtcNow,
            sortOrder: 0);

        Assert.False(item.IsChecked);
        Assert.Null(item.CheckedOnUtc);
    }

    [Fact]
    public void ShoppingListItem_UpdateDetails_WithNegativeSortOrder_Throws() {
        var list = ShoppingList.Create(UserId.New(), "Weekly");
        ShoppingListItem item = list.AddItem(
            name: "Milk",
            productId: null,
            amount: 1,
            unit: null,
            category: null,
            isChecked: false,
            sortOrder: 0);

        Assert.Throws<ArgumentOutOfRangeException>(() => item.UpdateDetails(
            name: "Milk",
            productId: null,
            amount: 1,
            unit: null,
            category: null,
            aisle: null,
            note: null,
            isChecked: false,
            checkedOnUtc: null,
            sortOrder: -1));
    }

    [Fact]
    public void ShoppingListItem_UpdateDetails_WithEmptyProductId_Throws() {
        var list = ShoppingList.Create(UserId.New(), "Weekly");
        ShoppingListItem item = list.AddItem(
            name: "Milk",
            productId: null,
            amount: 1,
            unit: null,
            category: null,
            isChecked: false,
            sortOrder: 0);

        Assert.Throws<ArgumentException>(() => item.UpdateDetails(
            name: "Milk",
            productId: ProductId.Empty,
            amount: 1,
            unit: null,
            category: null,
            aisle: null,
            note: null,
            isChecked: false,
            checkedOnUtc: null,
            sortOrder: 0));
    }

    [Fact]
    public void ShoppingListItem_UpdateDetails_WithBlankName_Throws() {
        var list = ShoppingList.Create(UserId.New(), "Weekly");
        ShoppingListItem item = list.AddItem(
            name: "Milk",
            productId: null,
            amount: 1,
            unit: null,
            category: null,
            isChecked: false,
            sortOrder: 0);

        Assert.Throws<ArgumentException>(() => item.UpdateDetails(
            name: "   ",
            productId: null,
            amount: 1,
            unit: null,
            category: null,
            aisle: null,
            note: null,
            isChecked: false,
            checkedOnUtc: null,
            sortOrder: 0));
    }

    [Fact]
    public void ShoppingListItemSource_CreateMealPlanSource_NormalizesValues() {
        var source = ShoppingListItemSource.CreateMealPlanSource(
            ShoppingListItemId.New(),
            MealPlanId.New(),
            MealPlanMealId.New(),
            RecipeId.New(),
            label: "  Dinner prep  ",
            dayNumber: 2,
            mealType: "   ",
            amount: 3,
            unit: MeasurementUnit.Pcs);

        Assert.Multiple(
            () => Assert.Equal(ShoppingListItemSourceType.MealPlan, source.SourceType),
            () => Assert.Equal("Dinner prep", source.Label),
            () => Assert.Equal(2, source.DayNumber),
            () => Assert.Null(source.MealType),
            () => Assert.Equal(3, source.Amount),
            () => Assert.Equal(MeasurementUnit.Pcs, source.Unit),
            () => Assert.NotEqual(ShoppingListItemSourceId.Empty, source.Id));
    }

    [Fact]
    public void ShoppingListItemSource_CreateMealPlanSource_WithInvalidDayNumber_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() => ShoppingListItemSource.CreateMealPlanSource(
            ShoppingListItemId.New(),
            MealPlanId.New(),
            MealPlanMealId.New(),
            RecipeId.New(),
            label: "Dinner prep",
            dayNumber: 0,
            mealType: "Dinner",
            amount: 3,
            unit: MeasurementUnit.Pcs));
    }

    [Fact]
    public void ShoppingListItemSource_CreateMealPlanSource_WithBlankLabel_Throws() {
        Assert.Throws<ArgumentException>(() => ShoppingListItemSource.CreateMealPlanSource(
            ShoppingListItemId.New(),
            MealPlanId.New(),
            MealPlanMealId.New(),
            RecipeId.New(),
            label: "   ",
            dayNumber: 1,
            mealType: "Dinner",
            amount: 3,
            unit: MeasurementUnit.Pcs));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void ShoppingListItemSource_CreateMealPlanSource_WithNonFiniteAmount_Throws(double amount) {
        Assert.Throws<ArgumentOutOfRangeException>(() => ShoppingListItemSource.CreateMealPlanSource(
            ShoppingListItemId.New(),
            MealPlanId.New(),
            MealPlanMealId.New(),
            RecipeId.New(),
            label: "Dinner prep",
            dayNumber: 1,
            mealType: "Dinner",
            amount: amount,
            unit: MeasurementUnit.Pcs));
    }

    [Fact]
    public void ShoppingListItemSource_CreateMealPlanSource_WithEmptyShoppingListItemId_Throws() {
        Assert.Throws<ArgumentException>(() => ShoppingListItemSource.CreateMealPlanSource(
            ShoppingListItemId.Empty,
            MealPlanId.New(),
            MealPlanMealId.New(),
            RecipeId.New(),
            label: "Dinner prep",
            dayNumber: 1,
            mealType: "Dinner",
            amount: 3,
            unit: MeasurementUnit.Pcs));
    }

    [Fact]
    public void ShoppingListItemSource_CreateMealPlanSource_WithEmptyMealPlanId_Throws() {
        Assert.Throws<ArgumentException>(() => ShoppingListItemSource.CreateMealPlanSource(
            ShoppingListItemId.New(),
            MealPlanId.Empty,
            MealPlanMealId.New(),
            RecipeId.New(),
            label: "Dinner prep",
            dayNumber: 1,
            mealType: "Dinner",
            amount: 3,
            unit: MeasurementUnit.Pcs));
    }

    [Fact]
    public void ShoppingListItemSource_CreateMealPlanSource_WithEmptyMealPlanMealId_Throws() {
        Assert.Throws<ArgumentException>(() => ShoppingListItemSource.CreateMealPlanSource(
            ShoppingListItemId.New(),
            MealPlanId.New(),
            MealPlanMealId.Empty,
            RecipeId.New(),
            label: "Dinner prep",
            dayNumber: 1,
            mealType: "Dinner",
            amount: 3,
            unit: MeasurementUnit.Pcs));
    }

    [Fact]
    public void ShoppingListItemSource_CreateMealPlanSource_WithEmptyRecipeId_Throws() {
        Assert.Throws<ArgumentException>(() => ShoppingListItemSource.CreateMealPlanSource(
            ShoppingListItemId.New(),
            MealPlanId.New(),
            MealPlanMealId.New(),
            RecipeId.Empty,
            label: "Dinner prep",
            dayNumber: 1,
            mealType: "Dinner",
            amount: 3,
            unit: MeasurementUnit.Pcs));
    }

    [Fact]
    public void UpdateName_WithDifferentValue_RaisesNameUpdatedDomainEvent() {
        var list = ShoppingList.Create(UserId.New(), "Weekly");

        list.UpdateName("Monthly");

        ShoppingListNameUpdatedDomainEvent evt = Assert.Single(list.DomainEvents.OfType<ShoppingListNameUpdatedDomainEvent>());
        Assert.Multiple(
            () => Assert.Equal(list.Id, evt.ShoppingListId),
            () => Assert.Equal("Weekly", evt.PreviousName),
            () => Assert.Equal("Monthly", evt.CurrentName));
    }

    [Fact]
    public void AddItem_RaisesItemAddedDomainEvent() {
        var list = ShoppingList.Create(UserId.New(), "Weekly");

        ShoppingListItem item = list.AddItem(
            name: "Milk",
            productId: null,
            amount: 1,
            unit: null,
            category: "Dairy",
            isChecked: false,
            sortOrder: 1);

        ShoppingListItemAddedDomainEvent evt = Assert.Single(list.DomainEvents.OfType<ShoppingListItemAddedDomainEvent>());
        Assert.Multiple(
            () => Assert.Equal(list.Id, evt.ShoppingListId),
            () => Assert.Equal(item.Id, evt.ShoppingListItemId),
            () => Assert.Equal("Milk", evt.Name),
            () => Assert.Equal("Dairy", evt.Category),
            () => Assert.Equal(1, evt.SortOrder));
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

        ShoppingListItemsClearedDomainEvent evt = Assert.Single(list.DomainEvents.OfType<ShoppingListItemsClearedDomainEvent>());
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
