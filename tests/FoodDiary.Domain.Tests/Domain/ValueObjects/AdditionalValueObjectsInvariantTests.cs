using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Domain.Tests.Domain.ValueObjects;

[ExcludeFromCodeCoverage]
public class AdditionalValueObjectsInvariantTests {
    // --- MealAiItemState ---

    // --- FoodQualityScore ---

    [Fact]
    public void FoodQualityScore_Calculate_WithZeroCalories_ReturnsYellow50() {
        var result = FoodQualityScore.Calculate(0, 0, 0, 0, 0, 0);

        Assert.Equal(50, result.Score);
        Assert.Equal(FoodQualityGrade.Yellow, result.Grade);
    }

    [Fact]
    public void FoodQualityScore_Calculate_HighProteinLowCalDensity_ReturnsHighScore() {
        // High protein, high fiber, low calorie density = healthy food
        var result = FoodQualityScore.Calculate(
            caloriesPerBase: 50, proteinsPerBase: 10, fatsPerBase: 1,
            carbsPerBase: 5, fiberPerBase: 5, alcoholPerBase: 0,
            productType: ProductType.Vegetable);

        Assert.True(result.Score >= 67);
        Assert.Equal(FoodQualityGrade.Green, result.Grade);
    }

    [Fact]
    public void FoodQualityScore_Calculate_HighCalDensityWithAlcohol_ReturnsLowScore() {
        var result = FoodQualityScore.Calculate(
            caloriesPerBase: 500, proteinsPerBase: 0, fatsPerBase: 0,
            carbsPerBase: 10, fiberPerBase: 0, alcoholPerBase: 50,
            productType: ProductType.Beverage);

        Assert.True(result.Score < 34);
        Assert.Equal(FoodQualityGrade.Red, result.Grade);
    }

    [Fact]
    public void FoodQualityScore_Calculate_VegetableModifier_IncreasesScore() {
        var withoutType = FoodQualityScore.Calculate(100, 5, 2, 10, 3, 0, ProductType.Unknown);
        var withVegetable = FoodQualityScore.Calculate(100, 5, 2, 10, 3, 0, ProductType.Vegetable);

        Assert.True(withVegetable.Score > withoutType.Score);
    }

    [Fact]
    public void FoodQualityScore_Calculate_DessertModifier_DecreasesScore() {
        var withoutType = FoodQualityScore.Calculate(300, 5, 10, 40, 1, 0, ProductType.Unknown);
        var withDessert = FoodQualityScore.Calculate(300, 5, 10, 40, 1, 0, ProductType.Dessert);

        Assert.True(withDessert.Score < withoutType.Score);
    }

    [Theory]
    [InlineData(ProductType.Fruit)]
    [InlineData(ProductType.Seafood)]
    [InlineData(ProductType.Meat)]
    [InlineData(ProductType.Dairy)]
    [InlineData(ProductType.Grain)]
    public void FoodQualityScore_Calculate_PositiveProductTypeModifiers_IncreaseScore(ProductType productType) {
        var withoutType = FoodQualityScore.Calculate(180, 8, 4, 20, 3, 0, ProductType.Unknown);
        var withType = FoodQualityScore.Calculate(180, 8, 4, 20, 3, 0, productType);

        Assert.True(withType.Score > withoutType.Score);
    }

    [Theory]
    [InlineData(ProductType.Cheese)]
    [InlineData(ProductType.Beverage)]
    public void FoodQualityScore_Calculate_NegativeProductTypeModifiers_DecreaseScore(ProductType productType) {
        var withoutType = FoodQualityScore.Calculate(180, 8, 4, 20, 3, 0, ProductType.Unknown);
        var withType = FoodQualityScore.Calculate(180, 8, 4, 20, 3, 0, productType);

        Assert.True(withType.Score < withoutType.Score);
    }

    [Fact]
    public void FoodQualityScore_Calculate_ScoreIsClampedTo0_100() {
        var result = FoodQualityScore.Calculate(
            caloriesPerBase: 10, proteinsPerBase: 50, fatsPerBase: 0,
            carbsPerBase: 0, fiberPerBase: 50, alcoholPerBase: 0,
            productType: ProductType.Vegetable);

        Assert.InRange(result.Score, 0, 100);
    }

    // --- HealthAreaScores ---

    [Fact]
    public void HealthAreaScores_Calculate_WithEmptyDictionaries_ReturnsUnknown() {
        var result = HealthAreaScores.Calculate(
            new Dictionary<int, double>(),
            new Dictionary<int, double>());

        Assert.Multiple(
            () => Assert.Equal(HealthAreaGrade.Unknown, result.Heart.Grade),
            () => Assert.Equal(HealthAreaGrade.Unknown, result.Bone.Grade),
            () => Assert.Equal(HealthAreaGrade.Unknown, result.Immune.Grade),
            () => Assert.Equal(HealthAreaGrade.Unknown, result.Energy.Grade),
            () => Assert.Equal(HealthAreaGrade.Unknown, result.Antioxidant.Grade));
    }

    [Fact]
    public void HealthAreaScores_Calculate_WithGoodNutrientAmounts_ReturnsHighScores() {
        // Provide 100% of daily values for heart nutrients (Potassium=1092, Magnesium=1090)
        var amounts = new Dictionary<int, double> {
            [1092] = 4700,  // Potassium
            [1090] = 420,   // Magnesium
        };
        var dailyValues = new Dictionary<int, double> {
            [1092] = 4700,
            [1090] = 420,
        };

        var result = HealthAreaScores.Calculate(amounts, dailyValues);

        Assert.True(result.Heart.Score >= 75);
        Assert.Equal(HealthAreaGrade.Excellent, result.Heart.Grade);
    }

    [Fact]
    public void HealthAreaScores_Calculate_WithExcessSodium_PenalizesHeart() {
        var amountsLowSodium = new Dictionary<int, double> {
            [1092] = 4700,
            [1090] = 420,
            [1093] = 1000,  // Sodium under limit
        };
        var amountsHighSodium = new Dictionary<int, double> {
            [1092] = 4700,
            [1090] = 420,
            [1093] = 5000,  // Sodium way over limit
        };
        var dailyValues = new Dictionary<int, double> {
            [1092] = 4700,
            [1090] = 420,
            [1093] = 2300,  // Sodium DV
        };

        var lowSodium = HealthAreaScores.Calculate(amountsLowSodium, dailyValues);
        var highSodium = HealthAreaScores.Calculate(amountsHighSodium, dailyValues);

        Assert.True(lowSodium.Heart.Score > highSodium.Heart.Score);
    }

    [Fact]
    public void HealthAreaScores_Calculate_ScoreIsClampedTo0_100() {
        var amounts = new Dictionary<int, double> {
            [1092] = 10000,
            [1090] = 10000,
        };
        var dailyValues = new Dictionary<int, double> {
            [1092] = 100,
            [1090] = 100,
        };

        var result = HealthAreaScores.Calculate(amounts, dailyValues);

        Assert.InRange(result.Heart.Score, 0, 100);
    }

    [Fact]
    public void HealthAreaScores_Calculate_WithPartialDailyValues_SkipsMissingAndInvalidDailyValues() {
        var amounts = new Dictionary<int, double> {
            [1092] = 2350,
            [1090] = 420,
        };
        var dailyValues = new Dictionary<int, double> {
            [1092] = 4700,
            [1090] = 0,
        };

        var result = HealthAreaScores.Calculate(amounts, dailyValues);

        Assert.Equal(50, result.Heart.Score);
        Assert.Equal(HealthAreaGrade.Good, result.Heart.Grade);
    }

    [Fact]
    public void HealthAreaScores_Calculate_WithLowAmount_ReturnsLowGrade() {
        var amounts = new Dictionary<int, double> {
            [1092] = 100,
            [1090] = 10,
        };
        var dailyValues = new Dictionary<int, double> {
            [1092] = 4700,
            [1090] = 420,
        };

        var result = HealthAreaScores.Calculate(amounts, dailyValues);

        Assert.Equal(HealthAreaGrade.Low, result.Heart.Grade);
    }

    [Fact]
    public void HealthAreaScores_Calculate_WithFairAmount_ReturnsFairGrade() {
        var amounts = new Dictionary<int, double> {
            [1092] = 1410,
            [1090] = 126,
        };
        var dailyValues = new Dictionary<int, double> {
            [1092] = 4700,
            [1090] = 420,
        };

        var result = HealthAreaScores.Calculate(amounts, dailyValues);

        Assert.Equal(HealthAreaGrade.Fair, result.Heart.Grade);
    }

    [Fact]
    public void HealthAreaScores_Calculate_WithZeroAmountAndKnownDailyValues_ReturnsUnknownGrade() {
        var amounts = new Dictionary<int, double>();
        var dailyValues = new Dictionary<int, double> {
            [1092] = 4700,
            [1090] = 420,
        };

        var result = HealthAreaScores.Calculate(amounts, dailyValues);

        Assert.Equal(0, result.Heart.Score);
        Assert.Equal(HealthAreaGrade.Unknown, result.Heart.Grade);
    }

    // --- RecipeStepContentState ---

    [Fact]
    public void RecipeStepContentState_Create_WithBlankInstruction_Throws() {
        Assert.Throws<ArgumentException>(() =>
            RecipeStepContentState.Create("   "));
    }

    [Fact]
    public void RecipeStepContentState_Create_WithTooLongInstruction_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RecipeStepContentState.Create(new string('i', 4001)));
    }

    [Fact]
    public void RecipeStepContentState_Create_WithTooLongTitle_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RecipeStepContentState.Create("Mix ingredients", title: new string('t', 257)));
    }

    [Fact]
    public void RecipeStepContentState_Create_NormalizesValues() {
        var state = RecipeStepContentState.Create(
            "  Mix ingredients  ", title: "  Step 1  ");

        Assert.Equal("Step 1", state.Title);
        Assert.Equal("Mix ingredients", state.Instruction);
    }

    [Fact]
    public void RecipeStepContentState_Create_WithWhitespaceTitle_SetsNull() {
        var state = RecipeStepContentState.Create("Mix", title: "   ");

        Assert.Null(state.Title);
    }

    // --- UserAccountState ---

    // --- UserAiQuotaState ---

    // --- UserSecurityState ---

    // --- RecipeNutrition ---

    [Fact]
    public void RecipeNutrition_Create_WithNegativeValue_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RecipeNutrition.Create(-1, proteins: null, fats: null, carbs: null, fiber: null, alcohol: null));
    }

    [Fact]
    public void RecipeNutrition_Create_WithNullValues_Succeeds() {
        var nutrition = RecipeNutrition.Create(calories: null, proteins: null, fats: null, carbs: null, fiber: null, alcohol: null);

        Assert.Null(nutrition.Calories);
        Assert.Null(nutrition.Proteins);
    }

    [Fact]
    public void RecipeNutrition_Create_WithValidValues_StoresAll() {
        var nutrition = RecipeNutrition.Create(500, 30, 20, 50, 5, 0);

        Assert.Multiple(
            () => Assert.Equal(500, nutrition.Calories),
            () => Assert.Equal(30, nutrition.Proteins),
            () => Assert.Equal(20, nutrition.Fats),
            () => Assert.Equal(50, nutrition.Carbs),
            () => Assert.Equal(5, nutrition.Fiber),
            () => Assert.Equal(0, nutrition.Alcohol));
    }

    [Theory]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void RecipeNutrition_Create_WithInfiniteValue_Throws(double value) {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RecipeNutrition.Create(value, proteins: null, fats: null, carbs: null, fiber: null, alcohol: null));
    }

    // --- UserNutritionGoals.With ---

    // --- UserActivityGoals ---

    // --- DietologistPermissions ---

    [Fact]
    public void DietologistPermissions_AllEnabled_AllFieldsTrue() {
        DietologistPermissions perms = DietologistPermissions.AllEnabled;

        Assert.Multiple(
            () => Assert.True(perms.ShareMeals),
            () => Assert.True(perms.ShareStatistics),
            () => Assert.True(perms.ShareWeight),
            () => Assert.True(perms.ShareWaist),
            () => Assert.True(perms.ShareGoals),
            () => Assert.True(perms.ShareHydration),
            () => Assert.True(perms.ShareProfile),
            () => Assert.True(perms.ShareFasting));
    }

    [Fact]
    public void DietologistPermissions_WithSelectiveDisable_PreservesOthers() {
        var perms = new DietologistPermissions(ShareMeals: false, ShareWeight: false);

        Assert.Multiple(
            () => Assert.False(perms.ShareMeals),
            () => Assert.True(perms.ShareStatistics),
            () => Assert.False(perms.ShareWeight),
            () => Assert.True(perms.ShareWaist),
            () => Assert.True(perms.ShareGoals),
            () => Assert.True(perms.ShareHydration),
            () => Assert.True(perms.ShareProfile),
            () => Assert.True(perms.ShareFasting));
    }

    // --- Simple code value objects ---

    [Theory]
    [InlineData(" en ", "en")]
    [InlineData("RU", "ru")]
    public void LanguageCode_TryParse_WithSupportedValues_Normalizes(string value, string expected) {
        bool parsed = LanguageCode.TryParse(value, out LanguageCode language);

        Assert.Multiple(
            () => Assert.True(parsed),
            () => Assert.Equal(expected, language.Value),
            () => Assert.Equal(expected, language.ToString()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("de")]
    public void LanguageCode_TryParse_WithUnsupportedValues_ReturnsFalse(string? value) {
        bool parsed = LanguageCode.TryParse(value, out LanguageCode language);

        Assert.False(parsed);
        Assert.Equal(default, language);
    }

    [Theory]
    [InlineData(null, "en")]
    [InlineData(" ", "en")]
    [InlineData("ru-RU", "ru")]
    [InlineData("en-US", "en")]
    public void LanguageCode_FromPreferred_ReturnsSupportedLanguage(string? value, string expected) {
        var language = LanguageCode.FromPreferred(value);

        Assert.Equal(expected, language.Value);
    }

    // --- EmailAddress ---

    [Fact]
    public void EmailAddress_Create_NormalizesAndToStringReturnsValue() {
        var email = EmailAddress.Create("  USER@Example.COM  ");

        Assert.Equal("user@example.com", email.Value);
        Assert.Equal(email.Value, email.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("not-an-email")]
    [InlineData("a@b@c")]
    public void EmailAddress_Create_WithInvalidValue_Throws(string value) {
        Assert.Throws<ArgumentException>(() => EmailAddress.Create(value));
    }

    [Fact]
    public void EmailAddress_Create_WithDisplayName_Throws() {
        Assert.Throws<ArgumentException>(() => EmailAddress.Create("User <user@example.com>"));
    }

    // --- Desired values ---

    [Fact]
    public void DesiredWeightAndWaist_Create_ExposeValues() {
        var weight = DesiredWeightKg.Create(75.5);
        var waist = DesiredWaistCm.Create(82.3);

        Assert.Equal(75.5, weight.Value);
        Assert.Equal(82.3, waist.Value);
    }
}
