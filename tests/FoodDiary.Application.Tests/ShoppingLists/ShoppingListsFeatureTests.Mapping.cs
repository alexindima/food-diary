using FoodDiary.Application.Abstractions.ShoppingLists.Models;
using FoodDiary.Application.ShoppingLists.Mappings;
using FoodDiary.Domain.Entities.Shopping;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.ShoppingLists.Models;

namespace FoodDiary.Application.Tests.ShoppingLists;

public partial class ShoppingListsFeatureTests {

    [Fact]
    public void ShoppingListMappings_ReadModelToModel_MapsAndSortsSources() {
        var listId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var firstSource = new ShoppingListItemSourceReadModel(
            Guid.NewGuid(),
            "MealPlan",
            MealPlanId: Guid.NewGuid(),
            MealPlanMealId: Guid.NewGuid(),
            RecipeId: Guid.NewGuid(),
            "Breakfast",
            DayNumber: 1,
            MealType: "Breakfast",
            Amount: 2,
            Unit: "Pcs");
        var secondSource = new ShoppingListItemSourceReadModel(
            Guid.NewGuid(),
            "MealPlan",
            MealPlanId: Guid.NewGuid(),
            MealPlanMealId: Guid.NewGuid(),
            RecipeId: Guid.NewGuid(),
            "Any day",
            DayNumber: null,
            MealType: null,
            Amount: 1,
            Unit: null);
        var list = new ShoppingListReadModel(
            listId,
            "Weekly",
            new DateTime(2026, 7, 8, 10, 0, 0, DateTimeKind.Utc),
            [
                new ShoppingListItemReadModel(
                    itemId,
                    listId,
                    ProductId: null,
                    "Eggs",
                    Amount: 2,
                    Unit: "Pcs",
                    Category: "Protein",
                    Aisle: "Cold",
                    Note: "organic",
                    IsChecked: true,
                    CheckedOnUtc: new DateTime(2026, 7, 8, 12, 0, 0, DateTimeKind.Utc),
                    SortOrder: 1,
                    Sources: [secondSource, firstSource]),
            ]);

        ShoppingListModel model = list.ToModel();

        ShoppingListItemModel item = Assert.Single(model.Items);
        Assert.Equal(itemId, item.Id);
        Assert.Equal("Eggs", item.Name);
        Assert.Equal([firstSource.Id, secondSource.Id], [.. item.Sources.Select(source => source.Id)]);
        Assert.Equal(firstSource.MealPlanId, item.Sources[0].MealPlanId);
        Assert.Equal(firstSource.MealPlanMealId, item.Sources[0].MealPlanMealId);
        Assert.Equal(firstSource.RecipeId, item.Sources[0].RecipeId);
        Assert.Equal("Breakfast", item.Sources[0].MealType);
        Assert.Equal(2, item.Sources[0].Amount);
        Assert.Equal("Pcs", item.Sources[0].Unit);
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
}
