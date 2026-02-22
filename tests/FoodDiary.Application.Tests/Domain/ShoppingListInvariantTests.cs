using FoodDiary.Domain.Entities;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Tests.Domain;

public class ShoppingListInvariantTests
{
    [Fact]
    public void Create_WithBlankName_Throws()
    {
        Assert.Throws<ArgumentException>(() => ShoppingList.Create(UserId.New(), "   "));
    }

    [Fact]
    public void AddItem_WithNegativeSortOrder_Throws()
    {
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
}
