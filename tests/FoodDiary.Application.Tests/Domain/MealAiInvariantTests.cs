using FoodDiary.Domain.Entities.Meals;

namespace FoodDiary.Application.Tests.Domain;

public class MealAiInvariantTests {
    [Fact]
    public void MealAiItemData_Create_WithValidValues_Succeeds() {
        var data = MealAiItemData.Create(
            "Chicken", "Курица", 100, "g", 165, 31, 3.6, 0, 0, 0);

        Assert.Equal("Chicken", data.NameEn);
        Assert.Equal("Курица", data.NameLocal);
        Assert.Equal(100, data.Amount);
        Assert.Equal("g", data.Unit);
    }

    [Fact]
    public void MealAiItemData_Create_WithBlankNameEn_Throws() {
        Assert.Throws<ArgumentException>(() =>
            MealAiItemData.Create("   ", null, 100, "g", 100, 10, 5, 20, 3, 0));
    }

    [Fact]
    public void MealAiItemData_Create_WithZeroAmount_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            MealAiItemData.Create("Chicken", null, 0, "g", 100, 10, 5, 20, 3, 0));
    }

    [Fact]
    public void MealAiItemData_Create_WithNegativeCalories_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            MealAiItemData.Create("Chicken", null, 100, "g", -1, 10, 5, 20, 3, 0));
    }

    [Fact]
    public void MealAiItemData_Create_WithNaNCalories_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            MealAiItemData.Create("Chicken", null, 100, "g", double.NaN, 10, 5, 20, 3, 0));
    }

    [Fact]
    public void MealAiItemData_Create_WithBlankUnit_Throws() {
        Assert.Throws<ArgumentException>(() =>
            MealAiItemData.Create("Chicken", null, 100, "   ", 100, 10, 5, 20, 3, 0));
    }

    [Fact]
    public void MealAiItemData_Create_WithTooLongNameEn_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            MealAiItemData.Create(new string('a', 257), null, 100, "g", 100, 10, 5, 20, 3, 0));
    }

    [Fact]
    public void MealAiItemData_Create_WithTooLongUnit_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            MealAiItemData.Create("Chicken", null, 100, new string('u', 33), 100, 10, 5, 20, 3, 0));
    }

    [Fact]
    public void MealAiItemData_TryCreate_WithValidValues_ReturnsTrue() {
        var result = MealAiItemData.TryCreate(
            "Chicken", null, 100, "g", 165, 31, 3.6, 0, 0, 0,
            out var data, out var error);

        Assert.True(result);
        Assert.NotNull(data);
        Assert.Null(error);
    }

    [Fact]
    public void MealAiItemData_TryCreate_WithInvalidValues_ReturnsFalse() {
        var result = MealAiItemData.TryCreate(
            "   ", null, 100, "g", 165, 31, 3.6, 0, 0, 0,
            out var data, out var error);

        Assert.False(result);
        Assert.Null(data);
        Assert.NotNull(error);
    }

    [Fact]
    public void MealAiItemData_Create_WithWhitespaceNameLocal_SetsNull() {
        var data = MealAiItemData.Create(
            "Chicken", "   ", 100, "g", 165, 31, 3.6, 0, 2, 0);

        Assert.Null(data.NameLocal);
    }
}
