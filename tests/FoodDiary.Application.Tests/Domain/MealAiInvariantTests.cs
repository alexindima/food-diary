using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Tests.Domain;

[ExcludeFromCodeCoverage]
public class MealAiInvariantTests {
    [Fact]
    public void MealAiItemData_Create_WithValidValues_Succeeds() {
        var data = MealAiItemData.Create(
            "Chicken", "ÐšÑƒÑ€Ð¸Ñ†Ð°", 100, "g", 165, 31, 3.6, 0, 0, 0);

        Assert.Equal("Chicken", data.NameEn);
        Assert.Equal("ÐšÑƒÑ€Ð¸Ñ†Ð°", data.NameLocal);
        Assert.Equal(100, data.Amount);
        Assert.Equal("g", data.Unit);
    }

    [Fact]
    public void MealAiItemData_Create_WithBlankNameEn_Throws() {
        Assert.Throws<ArgumentException>(() =>
            MealAiItemData.Create("   ", nameLocal: null, 100, "g", 100, 10, 5, 20, 3, 0));
    }

    [Fact]
    public void MealAiItemData_Create_WithZeroAmount_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            MealAiItemData.Create("Chicken", nameLocal: null, 0, "g", 100, 10, 5, 20, 3, 0));
    }

    [Fact]
    public void MealAiItemData_Create_WithNegativeCalories_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            MealAiItemData.Create("Chicken", nameLocal: null, 100, "g", -1, 10, 5, 20, 3, 0));
    }

    [Fact]
    public void MealAiItemData_Create_WithNaNCalories_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            MealAiItemData.Create("Chicken", nameLocal: null, 100, "g", double.NaN, 10, 5, 20, 3, 0));
    }

    [Fact]
    public void MealAiItemData_Create_WithBlankUnit_Throws() {
        Assert.Throws<ArgumentException>(() =>
            MealAiItemData.Create("Chicken", nameLocal: null, 100, "   ", 100, 10, 5, 20, 3, 0));
    }

    [Fact]
    public void MealAiItemData_Create_WithTooLongNameEn_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            MealAiItemData.Create(new string('a', 257), nameLocal: null, 100, "g", 100, 10, 5, 20, 3, 0));
    }

    [Fact]
    public void MealAiItemData_Create_WithTooLongUnit_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            MealAiItemData.Create("Chicken", nameLocal: null, 100, new string('u', 33), 100, 10, 5, 20, 3, 0));
    }

    [Fact]
    public void MealAiItemData_Create_WithConfidenceOutOfRange_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            MealAiItemData.Create("Chicken", nameLocal: null, 100, "g", 100, 10, 5, 20, 3, 0, confidence: 1.1));
    }

    [Fact]
    public void MealAiItemData_Create_WithUnknownResolution_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            MealAiItemData.Create(
                "Chicken",
                nameLocal: null,
                100,
                "g",
                100,
                10,
                5,
                20,
                3,
                0,
                resolution: (MealAiItemResolution)999));
    }

    [Fact]
    public void MealAiItemData_TryCreate_WithValidValues_ReturnsTrue() {
        bool result = MealAiItemData.TryCreate(
            "Chicken", nameLocal: null, 100, "g", 165, 31, 3.6, 0, 0, 0,
            out MealAiItemData? data, out string? error);

        Assert.True(result);
        Assert.NotNull(data);
        Assert.Null(error);
    }

    [Fact]
    public void MealAiItemData_TryCreate_WithInvalidValues_ReturnsFalse() {
        bool result = MealAiItemData.TryCreate(
            "   ", nameLocal: null, 100, "g", 165, 31, 3.6, 0, 0, 0,
            out MealAiItemData? data, out string? error);

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
