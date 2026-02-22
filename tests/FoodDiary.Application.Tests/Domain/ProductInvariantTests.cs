using FoodDiary.Domain.Entities.Ai;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.Entities.Content;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Entities.Shopping;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Tests.Domain;

public class ProductInvariantTests
{
    [Fact]
    public void Create_WithInvalidName_Throws()
    {
        Assert.Throws<ArgumentException>(() => Product.Create(
            UserId.New(),
            name: "   ",
            baseUnit: MeasurementUnit.G,
            baseAmount: 100,
            defaultPortionAmount: 100,
            caloriesPerBase: 100,
            proteinsPerBase: 10,
            fatsPerBase: 10,
            carbsPerBase: 10,
            fiberPerBase: 1,
            alcoholPerBase: 0));
    }

    [Fact]
    public void Create_WithNegativeNutrition_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Product.Create(
            UserId.New(),
            name: "Apple",
            baseUnit: MeasurementUnit.G,
            baseAmount: 100,
            defaultPortionAmount: 100,
            caloriesPerBase: -1,
            proteinsPerBase: 10,
            fatsPerBase: 10,
            carbsPerBase: 10,
            fiberPerBase: 1,
            alcoholPerBase: 0));
    }

    [Fact]
    public void Update_WithInvalidPortion_Throws()
    {
        var product = Product.Create(
            UserId.New(),
            name: "Apple",
            baseUnit: MeasurementUnit.G,
            baseAmount: 100,
            defaultPortionAmount: 100,
            caloriesPerBase: 52,
            proteinsPerBase: 0.3,
            fatsPerBase: 0.2,
            carbsPerBase: 14,
            fiberPerBase: 2.4,
            alcoholPerBase: 0);

        Assert.Throws<ArgumentOutOfRangeException>(() => product.Update(defaultPortionAmount: 0));
    }
}

