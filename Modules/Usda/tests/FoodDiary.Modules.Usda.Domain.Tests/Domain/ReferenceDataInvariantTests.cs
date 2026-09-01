using FoodDiary.Domain.Entities.Usda;

namespace FoodDiary.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public sealed class ReferenceDataInvariantTests {
    public static TheoryData<double> NonFiniteValues => [
        double.NaN,
        double.NegativeInfinity,
        double.PositiveInfinity,
    ];

    [Fact]
    public void DailyReferenceValue_ExposesConfiguredValues() {
        var nutrient = new UsdaNutrient {
            Id = 1008,
            Name = "Energy",
            UnitName = "kcal",
        };

        var value = new DailyReferenceValue {
            Id = 1,
            NutrientId = nutrient.Id,
            Value = 2000,
            Unit = "kcal",
            AgeGroup = "adult",
            Gender = "all",
            Nutrient = nutrient,
        };

        Assert.Multiple(
            () => Assert.Equal(1, value.Id),
            () => Assert.Equal(1008, value.NutrientId),
            () => Assert.Equal(2000, value.Value),
            () => Assert.Equal("kcal", value.Unit),
            () => Assert.Equal("adult", value.AgeGroup),
            () => Assert.Equal("all", value.Gender),
            () => Assert.Same(nutrient, value.Nutrient));
    }

    [Fact]
    public void UsdaFoodNutrient_ExposesConfiguredValues() {
        var food = new UsdaFood {
            FdcId = 1,
            Description = "Apple",
        };
        var nutrient = new UsdaNutrient {
            Id = 1008,
            Name = "Energy",
            UnitName = "kcal",
        };

        var foodNutrient = new UsdaFoodNutrient {
            Id = 10,
            FdcId = food.FdcId,
            NutrientId = nutrient.Id,
            Amount = 52,
            Food = food,
            Nutrient = nutrient,
        };

        Assert.Multiple(
            () => Assert.Equal(10, foodNutrient.Id),
            () => Assert.Equal(1, foodNutrient.FdcId),
            () => Assert.Equal(1008, foodNutrient.NutrientId),
            () => Assert.Equal(52, foodNutrient.Amount),
            () => Assert.Same(food, foodNutrient.Food),
            () => Assert.Same(nutrient, foodNutrient.Nutrient));
    }

    [Fact]
    public void UsdaFoodPortion_ExposesConfiguredValues() {
        var food = new UsdaFood {
            FdcId = 1,
            Description = "Apple",
        };

        var portion = new UsdaFoodPortion {
            Id = 20,
            FdcId = food.FdcId,
            Amount = 1,
            MeasureUnitName = "medium",
            GramWeight = 182,
            PortionDescription = "1 medium apple",
            Modifier = "with skin",
            Food = food,
        };

        Assert.Multiple(
            () => Assert.Equal(20, portion.Id),
            () => Assert.Equal(1, portion.FdcId),
            () => Assert.Equal(1, portion.Amount),
            () => Assert.Equal("medium", portion.MeasureUnitName),
            () => Assert.Equal(182, portion.GramWeight),
            () => Assert.Equal("1 medium apple", portion.PortionDescription),
            () => Assert.Equal("with skin", portion.Modifier),
            () => Assert.Same(food, portion.Food));
    }

    [Fact]
    public void UsdaFood_ExposesReadOnlyNavigationCollections() {
        var food = new UsdaFood { FdcId = 1, Description = "Apple" };

        Assert.Multiple(
            () => Assert.True(Assert.IsAssignableFrom<ICollection<UsdaFoodNutrient>>(food.FoodNutrients).IsReadOnly),
            () => Assert.True(Assert.IsAssignableFrom<ICollection<UsdaFoodPortion>>(food.FoodPortions).IsReadOnly));
    }

    [Theory]
    [MemberData(nameof(NonFiniteValues))]
    public void DailyReferenceValue_WithNonFiniteValue_Throws(double value) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DailyReferenceValue {
            Id = 1,
            NutrientId = 1,
            Value = value,
            Unit = "g",
            AgeGroup = "adult",
            Gender = "all",
        });
    }

    [Theory]
    [MemberData(nameof(NonFiniteValues))]
    public void UsdaFoodNutrient_WithNonFiniteAmount_Throws(double value) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new UsdaFoodNutrient {
            Id = 1,
            FdcId = 1,
            NutrientId = 1,
            Amount = value,
        });
    }

    [Theory]
    [MemberData(nameof(NonFiniteValues))]
    public void UsdaFoodPortion_WithNonFiniteAmount_Throws(double value) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new UsdaFoodPortion {
            Id = 1,
            FdcId = 1,
            Amount = value,
            MeasureUnitName = "serving",
            GramWeight = 1,
        });
    }

    [Theory]
    [MemberData(nameof(NonFiniteValues))]
    public void UsdaFoodPortion_WithNonFiniteGramWeight_Throws(double value) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new UsdaFoodPortion {
            Id = 1,
            FdcId = 1,
            Amount = 1,
            MeasureUnitName = "serving",
            GramWeight = value,
        });
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void UsdaReferenceEntities_WithNonPositiveIdentifiers_Throw(int identifier) {
        Assert.Multiple(
            () => Assert.Throws<ArgumentOutOfRangeException>(() => new UsdaFood {
                FdcId = identifier,
                Description = "Food",
            }),
            () => Assert.Throws<ArgumentOutOfRangeException>(() => new UsdaFood {
                FdcId = 1,
                Description = "Food",
                FoodCategoryId = identifier,
            }),
            () => Assert.Throws<ArgumentOutOfRangeException>(() => new UsdaNutrient {
                Id = identifier,
                Name = "Energy",
                UnitName = "kcal",
            }),
            () => Assert.Throws<ArgumentOutOfRangeException>(() => new DailyReferenceValue {
                Id = identifier,
                NutrientId = 1,
                Value = 1,
                Unit = "g",
                AgeGroup = "adult",
                Gender = "all",
            }),
            () => Assert.Throws<ArgumentOutOfRangeException>(() => new DailyReferenceValue {
                Id = 1,
                NutrientId = identifier,
                Value = 1,
                Unit = "g",
                AgeGroup = "adult",
                Gender = "all",
            }),
            () => Assert.Throws<ArgumentOutOfRangeException>(() => new UsdaFoodNutrient {
                Id = identifier,
                FdcId = 1,
                NutrientId = 1,
                Amount = 0,
            }),
            () => Assert.Throws<ArgumentOutOfRangeException>(() => new UsdaFoodNutrient {
                Id = 1,
                FdcId = identifier,
                NutrientId = 1,
                Amount = 0,
            }),
            () => Assert.Throws<ArgumentOutOfRangeException>(() => new UsdaFoodNutrient {
                Id = 1,
                FdcId = 1,
                NutrientId = identifier,
                Amount = 0,
            }),
            () => Assert.Throws<ArgumentOutOfRangeException>(() => new UsdaFoodPortion {
                Id = identifier,
                FdcId = 1,
                Amount = 1,
                MeasureUnitName = "serving",
                GramWeight = 1,
            }),
            () => Assert.Throws<ArgumentOutOfRangeException>(() => new UsdaFoodPortion {
                Id = 1,
                FdcId = identifier,
                Amount = 1,
                MeasureUnitName = "serving",
                GramWeight = 1,
            }));
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(-1d)]
    public void DailyReferenceValue_WithNonPositiveValue_Throws(double value) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DailyReferenceValue {
            Id = 1,
            NutrientId = 1,
            Value = value,
            Unit = "g",
            AgeGroup = "adult",
            Gender = "all",
        });
    }

    [Fact]
    public void UsdaFoodNutrient_WithZeroAmount_Succeeds() {
        var nutrient = new UsdaFoodNutrient {
            Id = 1,
            FdcId = 1,
            NutrientId = 1,
            Amount = 0,
        };

        Assert.Equal(0, nutrient.Amount);
    }

    [Fact]
    public void UsdaFoodNutrient_WithNegativeAmount_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new UsdaFoodNutrient {
            Id = 1,
            FdcId = 1,
            NutrientId = 1,
            Amount = -1,
        });
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(-1d)]
    public void UsdaFoodPortion_WithNonPositiveAmount_Throws(double value) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new UsdaFoodPortion {
            Id = 1,
            FdcId = 1,
            Amount = value,
            MeasureUnitName = "serving",
            GramWeight = 1,
        });
    }

    [Theory]
    [InlineData(0d)]
    [InlineData(-1d)]
    public void UsdaFoodPortion_WithNonPositiveGramWeight_Throws(double value) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new UsdaFoodPortion {
            Id = 1,
            FdcId = 1,
            Amount = 1,
            MeasureUnitName = "serving",
            GramWeight = value,
        });
    }

    [Fact]
    public void RequiredTextValues_AreTrimmed() {
        var food = new UsdaFood { FdcId = 1, Description = " Apple " };
        var nutrient = new UsdaNutrient { Id = 1, Name = " Energy ", UnitName = " kcal " };
        var portion = new UsdaFoodPortion {
            Id = 1,
            FdcId = 1,
            Amount = 1,
            MeasureUnitName = " serving ",
            GramWeight = 100,
        };
        var dailyValue = new DailyReferenceValue {
            Id = 1,
            NutrientId = 1,
            Value = 1,
            Unit = " g ",
            AgeGroup = " adult ",
            Gender = " all ",
        };

        Assert.Multiple(
            () => Assert.Equal("Apple", food.Description),
            () => Assert.Equal("Energy", nutrient.Name),
            () => Assert.Equal("kcal", nutrient.UnitName),
            () => Assert.Equal("serving", portion.MeasureUnitName),
            () => Assert.Equal("g", dailyValue.Unit),
            () => Assert.Equal("adult", dailyValue.AgeGroup),
            () => Assert.Equal("all", dailyValue.Gender));
    }

    [Fact]
    public void RequiredTextValues_RejectBlankOrTooLongInput() {
        Assert.Multiple(
            () => Assert.Throws<ArgumentException>(() => new UsdaFood { FdcId = 1, Description = " " }),
            () => Assert.Throws<ArgumentOutOfRangeException>(() => new UsdaFood {
                FdcId = 1,
                Description = new string('d', UsdaFood.DescriptionMaxLength + 1),
            }),
            () => Assert.Throws<ArgumentException>(() => new UsdaNutrient { Id = 1, Name = " ", UnitName = "g" }),
            () => Assert.Throws<ArgumentOutOfRangeException>(() => new UsdaNutrient {
                Id = 1,
                Name = "Energy",
                UnitName = new string('u', UsdaNutrient.UnitNameMaxLength + 1),
            }),
            () => Assert.Throws<ArgumentException>(() => new UsdaFoodPortion {
                Id = 1,
                FdcId = 1,
                Amount = 1,
                MeasureUnitName = " ",
                GramWeight = 1,
            }),
            () => Assert.Throws<ArgumentOutOfRangeException>(() => new DailyReferenceValue {
                Id = 1,
                NutrientId = 1,
                Value = 1,
                Unit = "g",
                AgeGroup = "adult",
                Gender = new string('g', DailyReferenceValue.GenderMaxLength + 1),
            }));
    }

    [Fact]
    public void OptionalTextValues_NormalizeBlankAndRejectTooLongInput() {
        var food = new UsdaFood { FdcId = 1, Description = "Apple", FoodCategory = " " };
        var portion = new UsdaFoodPortion {
            Id = 1,
            FdcId = 1,
            Amount = 1,
            MeasureUnitName = "serving",
            GramWeight = 1,
            PortionDescription = " ",
            Modifier = " sliced ",
        };

        Assert.Multiple(
            () => Assert.Null(food.FoodCategory),
            () => Assert.Null(portion.PortionDescription),
            () => Assert.Equal("sliced", portion.Modifier),
            () => Assert.Throws<ArgumentOutOfRangeException>(() => new UsdaFood {
                FdcId = 1,
                Description = "Apple",
                FoodCategory = new string('c', UsdaFood.FoodCategoryMaxLength + 1),
            }),
            () => Assert.Throws<ArgumentOutOfRangeException>(() => new UsdaFoodPortion {
                Id = 1,
                FdcId = 1,
                Amount = 1,
                MeasureUnitName = "serving",
                GramWeight = 1,
                PortionDescription = new string('p', UsdaFoodPortion.PortionDescriptionMaxLength + 1),
            }));
    }
}
