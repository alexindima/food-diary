using FoodDiary.Domain.Entities.Usda;

namespace FoodDiary.Application.Tests.Domain;

[ExcludeFromCodeCoverage]
public sealed class ReferenceDataInvariantTests {
    [Fact]
    public void DailyReferenceValue_ExposesConfiguredValues() {
        var nutrient = new UsdaNutrient {
            Id = 1008,
            Name = "Energy",
            UnitName = "kcal"
        };

        var value = new DailyReferenceValue {
            Id = 1,
            NutrientId = nutrient.Id,
            Value = 2000,
            Unit = "kcal",
            AgeGroup = "adult",
            Gender = "all",
            Nutrient = nutrient
        };

        Assert.Equal(1, value.Id);
        Assert.Equal(1008, value.NutrientId);
        Assert.Equal(2000, value.Value);
        Assert.Equal("kcal", value.Unit);
        Assert.Equal("adult", value.AgeGroup);
        Assert.Equal("all", value.Gender);
        Assert.Same(nutrient, value.Nutrient);
    }

    [Fact]
    public void UsdaFoodNutrient_ExposesConfiguredValues() {
        var food = new UsdaFood {
            FdcId = 1,
            Description = "Apple"
        };
        var nutrient = new UsdaNutrient {
            Id = 1008,
            Name = "Energy",
            UnitName = "kcal"
        };

        var foodNutrient = new UsdaFoodNutrient {
            Id = 10,
            FdcId = food.FdcId,
            NutrientId = nutrient.Id,
            Amount = 52,
            Food = food,
            Nutrient = nutrient
        };

        Assert.Equal(10, foodNutrient.Id);
        Assert.Equal(1, foodNutrient.FdcId);
        Assert.Equal(1008, foodNutrient.NutrientId);
        Assert.Equal(52, foodNutrient.Amount);
        Assert.Same(food, foodNutrient.Food);
        Assert.Same(nutrient, foodNutrient.Nutrient);
    }

    [Fact]
    public void UsdaFoodPortion_ExposesConfiguredValues() {
        var food = new UsdaFood {
            FdcId = 1,
            Description = "Apple"
        };

        var portion = new UsdaFoodPortion {
            Id = 20,
            FdcId = food.FdcId,
            Amount = 1,
            MeasureUnitName = "medium",
            GramWeight = 182,
            PortionDescription = "1 medium apple",
            Modifier = "with skin",
            Food = food
        };

        Assert.Equal(20, portion.Id);
        Assert.Equal(1, portion.FdcId);
        Assert.Equal(1, portion.Amount);
        Assert.Equal("medium", portion.MeasureUnitName);
        Assert.Equal(182, portion.GramWeight);
        Assert.Equal("1 medium apple", portion.PortionDescription);
        Assert.Equal("with skin", portion.Modifier);
        Assert.Same(food, portion.Food);
    }
}
