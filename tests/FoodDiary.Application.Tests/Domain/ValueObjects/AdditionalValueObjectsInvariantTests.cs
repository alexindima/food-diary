using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Tests.Domain.ValueObjects;

[ExcludeFromCodeCoverage]
public class AdditionalValueObjectsInvariantTests {
    // --- MealAiItemState ---

    [Fact]
    public void MealAiItemState_Create_WithBlankNameEn_Throws() {
        Assert.Throws<ArgumentException>(() =>
            MealAiItemState.Create("   ", nameLocal: null, 100, "g", 100, 10, 5, 20, 3, 0));
    }

    [Fact]
    public void MealAiItemState_Create_WithTooLongNameEn_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            MealAiItemState.Create(new string('a', 257), nameLocal: null, 100, "g", 100, 10, 5, 20, 3, 0));
    }

    [Fact]
    public void MealAiItemState_Create_WithZeroAmount_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            MealAiItemState.Create("Chicken", nameLocal: null, 0, "g", 100, 10, 5, 20, 3, 0));
    }

    [Fact]
    public void MealAiItemState_Create_WithNegativeAmount_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            MealAiItemState.Create("Chicken", nameLocal: null, -1, "g", 100, 10, 5, 20, 3, 0));
    }

    [Fact]
    public void MealAiItemState_Create_WithNaNAmount_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            MealAiItemState.Create("Chicken", nameLocal: null, double.NaN, "g", 100, 10, 5, 20, 3, 0));
    }

    [Fact]
    public void MealAiItemState_Create_WithNegativeNutrition_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            MealAiItemState.Create("Chicken", nameLocal: null, 100, "g", -1, 10, 5, 20, 3, 0));
    }

    [Fact]
    public void MealAiItemState_Create_WithInfiniteNutrition_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            MealAiItemState.Create("Chicken", nameLocal: null, 100, "g", double.PositiveInfinity, 10, 5, 20, 3, 0));
    }

    [Fact]
    public void MealAiItemState_Create_TrimsAndNormalizesText() {
        var state = MealAiItemState.Create(
            "  Chicken  ", "  ÐšÑƒÑ€Ð¸Ñ†Ð°  ", 100, "  g  ", 165, 31, 3.6, 0, 0, 0);

        Assert.Equal("Chicken", state.NameEn);
        Assert.Equal("ÐšÑƒÑ€Ð¸Ñ†Ð°", state.NameLocal);
        Assert.Equal("g", state.Unit);
    }

    [Fact]
    public void MealAiItemState_Create_WithWhitespaceNameLocal_SetsNull() {
        var state = MealAiItemState.Create(
            "Chicken", "   ", 100, "g", 165, 31, 3.6, 0, 0, 0);

        Assert.Null(state.NameLocal);
    }

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

        Assert.Equal(HealthAreaGrade.Unknown, result.Heart.Grade);
        Assert.Equal(HealthAreaGrade.Unknown, result.Bone.Grade);
        Assert.Equal(HealthAreaGrade.Unknown, result.Immune.Grade);
        Assert.Equal(HealthAreaGrade.Unknown, result.Energy.Grade);
        Assert.Equal(HealthAreaGrade.Unknown, result.Antioxidant.Grade);
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

    [Fact]
    public void UserAccountState_CreateInitial_ReturnsActiveState() {
        var state = UserAccountState.CreateInitial();

        Assert.True(state.IsActive);
        Assert.Null(state.TelegramUserId);
        Assert.Null(state.DeletedAt);
    }

    [Fact]
    public void UserProfileState_CreateInitial_ExposesProjectionStates() {
        var state = UserProfileState.CreateInitial();

        Assert.Equal(ActivityLevel.Moderate, state.ActivityLevel);
        Assert.Equal(ThemeCode.Default.Value, state.Theme);
        Assert.Equal(UiStyleCode.Default.Value, state.UiStyle);
        Assert.False(state.PushNotificationsEnabled);
        Assert.True(state.FastingPushNotificationsEnabled);
        Assert.True(state.SocialPushNotificationsEnabled);
        Assert.Equal(12, state.FastingCheckInReminderHours);
        Assert.Equal(20, state.FastingCheckInFollowUpReminderHours);
        Assert.Equal(state.Username, state.PersonalInfo.Username);
        Assert.Equal(state.ProfileImage, state.Media.ProfileImage);
        Assert.Equal(state.DashboardLayoutJson, state.Preferences.DashboardLayoutJson);
    }

    [Fact]
    public void UserAccountState_WithTelegram_SetsTelegramUserId() {
        UserAccountState state = UserAccountState.CreateInitial().WithTelegram(12345);

        Assert.Equal(12345, state.TelegramUserId);
    }

    [Fact]
    public void UserAccountState_Deactivate_SetsInactive() {
        UserAccountState state = UserAccountState.CreateInitial().Deactivate();

        Assert.False(state.IsActive);
    }

    [Fact]
    public void UserAccountState_Activate_SetsActive() {
        UserAccountState state = UserAccountState.CreateInitial()
            .Deactivate()
            .Activate();

        Assert.True(state.IsActive);
    }

    [Fact]
    public void UserAccountState_MarkDeleted_SetsDeletedAtAndInactive() {
        DateTime deletedAt = DateTime.UtcNow;
        UserAccountState state = UserAccountState.CreateInitial().MarkDeleted(deletedAt);

        Assert.Equal(deletedAt, state.DeletedAt);
        Assert.False(state.IsActive);
    }

    [Fact]
    public void UserAccountState_Restore_ClearsDeletedAtAndSetsActive() {
        UserAccountState state = UserAccountState.CreateInitial()
            .MarkDeleted(DateTime.UtcNow)
            .Restore();

        Assert.Null(state.DeletedAt);
        Assert.True(state.IsActive);
    }

    // --- UserAiQuotaState ---

    [Fact]
    public void UserAiQuotaState_CreateInitial_WithNegativeInputLimit_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            UserAiQuotaState.CreateInitial(-1, 1000));
    }

    [Fact]
    public void UserAiQuotaState_CreateInitial_WithNegativeOutputLimit_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            UserAiQuotaState.CreateInitial(1000, -1));
    }

    [Fact]
    public void UserAiQuotaState_CreateInitial_WithValidValues_Succeeds() {
        var state = UserAiQuotaState.CreateInitial(100_000, 50_000);

        Assert.Equal(100_000, state.AiInputTokenLimit);
        Assert.Equal(50_000, state.AiOutputTokenLimit);
    }

    [Fact]
    public void UserAiQuotaState_WithLimits_UpdatesOnlyProvidedValues() {
        var state = UserAiQuotaState.CreateInitial(100_000, 50_000);

        UserAiQuotaState updated = state.WithLimits(inputLimit: 200_000, outputLimit: null);

        Assert.Equal(200_000, updated.AiInputTokenLimit);
        Assert.Equal(50_000, updated.AiOutputTokenLimit);
    }

    [Fact]
    public void UserAiQuotaState_WithLimits_WithNegativeInput_Throws() {
        var state = UserAiQuotaState.CreateInitial(100_000, 50_000);

        Assert.Throws<ArgumentOutOfRangeException>(() => state.WithLimits(inputLimit: -1, outputLimit: null));
    }

    // --- UserSecurityState ---

    [Fact]
    public void UserSecurityState_CreateInitial_SetsPasswordAndDefaults() {
        var state = UserSecurityState.CreateInitial("hashed-password");

        Assert.Equal("hashed-password", state.Password);
        Assert.Null(state.RefreshToken);
        Assert.False(state.IsEmailConfirmed);
        Assert.Null(state.EmailConfirmationTokenHash);
        Assert.Null(state.PasswordResetTokenHash);
        Assert.Null(state.LastLoginAtUtc);
    }

    [Fact]
    public void UserSecurityState_WithPassword_UpdatesPassword() {
        UserSecurityState state = UserSecurityState.CreateInitial("old").WithPassword("new");

        Assert.Equal("new", state.Password);
    }

    [Fact]
    public void UserSecurityState_WithRefreshToken_SetsTokenAndLastLogin() {
        DateTime now = DateTime.UtcNow;
        UserSecurityState state = UserSecurityState.CreateInitial("hash")
            .WithRefreshToken("token", now);

        Assert.Equal("token", state.RefreshToken);
        Assert.Equal(now, state.LastLoginAtUtc);
    }

    [Fact]
    public void UserSecurityState_WithRefreshToken_WithNull_DoesNotUpdateLastLogin() {
        DateTime loginTime = DateTime.UtcNow;
        UserSecurityState state = UserSecurityState.CreateInitial("hash")
            .WithRefreshToken("token", loginTime)
            .WithRefreshToken(refreshToken: null, DateTime.UtcNow.AddHours(1));

        Assert.Null(state.RefreshToken);
        Assert.Equal(loginTime, state.LastLoginAtUtc);
    }

    [Fact]
    public void UserSecurityState_AsEmailConfirmed_ClearsConfirmationTokens() {
        UserSecurityState state = UserSecurityState.CreateInitial("hash")
            .WithEmailConfirmationToken("token-hash", DateTime.UtcNow.AddHours(1), DateTime.UtcNow)
            .AsEmailConfirmed(isConfirmed: true);

        Assert.True(state.IsEmailConfirmed);
        Assert.Null(state.EmailConfirmationTokenHash);
        Assert.Null(state.EmailConfirmationTokenExpiresAtUtc);
        Assert.Null(state.EmailConfirmationSentAtUtc);
    }

    [Fact]
    public void UserSecurityState_WithPasswordResetToken_SetsAllFields() {
        DateTime expires = DateTime.UtcNow.AddHours(1);
        DateTime now = DateTime.UtcNow;
        UserSecurityState state = UserSecurityState.CreateInitial("hash")
            .WithPasswordResetToken("reset-hash", expires, now);

        Assert.Equal("reset-hash", state.PasswordResetTokenHash);
        Assert.Equal(expires, state.PasswordResetTokenExpiresAtUtc);
        Assert.Equal(now, state.PasswordResetSentAtUtc);
    }

    [Fact]
    public void UserSecurityState_WithoutPasswordResetToken_ClearsResetFields() {
        UserSecurityState state = UserSecurityState.CreateInitial("hash")
            .WithPasswordResetToken("hash", DateTime.UtcNow.AddHours(1), DateTime.UtcNow)
            .WithoutPasswordResetToken();

        Assert.Null(state.PasswordResetTokenHash);
        Assert.Null(state.PasswordResetTokenExpiresAtUtc);
        Assert.Null(state.PasswordResetSentAtUtc);
    }

    [Fact]
    public void UserSecurityState_WithoutTransientTokens_ClearsAllTransientState() {
        UserSecurityState state = UserSecurityState.CreateInitial("hash")
            .WithRefreshToken("refresh", DateTime.UtcNow)
            .WithEmailConfirmationToken("confirm", DateTime.UtcNow.AddHours(1), DateTime.UtcNow)
            .WithPasswordResetToken("reset", DateTime.UtcNow.AddHours(1), DateTime.UtcNow)
            .WithoutTransientTokens();

        Assert.Null(state.RefreshToken);
        Assert.Null(state.EmailConfirmationTokenHash);
        Assert.Null(state.EmailConfirmationTokenExpiresAtUtc);
        Assert.Null(state.EmailConfirmationSentAtUtc);
        Assert.Null(state.PasswordResetTokenHash);
        Assert.Null(state.PasswordResetTokenExpiresAtUtc);
        Assert.Null(state.PasswordResetSentAtUtc);
    }

    // --- ProductNutrition.With ---

    [Fact]
    public void ProductNutrition_With_UpdatesOnlyProvidedValues() {
        var original = ProductNutrition.Create(100, 10, 5, 20, 3, 0);

        ProductNutrition updated = original.With(caloriesPerBase: 200);

        Assert.Equal(200, updated.CaloriesPerBase);
        Assert.Equal(10, updated.ProteinsPerBase);
        Assert.Equal(5, updated.FatsPerBase);
        Assert.Equal(20, updated.CarbsPerBase);
        Assert.Equal(3, updated.FiberPerBase);
        Assert.Equal(0, updated.AlcoholPerBase);
    }

    [Fact]
    public void ProductNutrition_With_WithNegativeValue_Throws() {
        var nutrition = ProductNutrition.Create(100, 10, 5, 20, 3, 0);

        Assert.Throws<ArgumentOutOfRangeException>(() => nutrition.With(fatsPerBase: -1));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void ProductNutrition_With_WithNonFiniteValue_Throws(double value) {
        var nutrition = ProductNutrition.Create(100, 10, 5, 20, 3, 0);

        Assert.Throws<ArgumentOutOfRangeException>(() => nutrition.With(proteinsPerBase: value));
    }

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

        Assert.Equal(500, nutrition.Calories);
        Assert.Equal(30, nutrition.Proteins);
        Assert.Equal(20, nutrition.Fats);
        Assert.Equal(50, nutrition.Carbs);
        Assert.Equal(5, nutrition.Fiber);
        Assert.Equal(0, nutrition.Alcohol);
    }

    [Theory]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void RecipeNutrition_Create_WithInfiniteValue_Throws(double value) {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            RecipeNutrition.Create(value, proteins: null, fats: null, carbs: null, fiber: null, alcohol: null));
    }

    // --- UserNutritionGoals.With ---

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void UserNutritionGoals_With_WithNonFiniteValue_Throws(double value) {
        var goals = UserNutritionGoals.Create(2000, 120, 70, 230, 30, 2.5);

        Assert.Throws<ArgumentOutOfRangeException>(() => goals.With(fatTarget: value));
    }

    [Fact]
    public void UserNutritionGoals_With_WithNegativeValue_Throws() {
        var goals = UserNutritionGoals.Create(2000, 120, 70, 230, 30, 2.5);

        Assert.Throws<ArgumentOutOfRangeException>(() => goals.With(carbTarget: -1));
    }

    [Fact]
    public void UserNutritionGoals_Create_WithNegativeValue_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            UserNutritionGoals.Create(-1, proteinTarget: null, fatTarget: null, carbTarget: null, fiberTarget: null, waterGoal: null));
    }

    // --- UserActivityGoals ---

    [Fact]
    public void UserActivityGoals_Create_WithNegativeStepGoal_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            UserActivityGoals.Create(-1, hydrationGoal: null));
    }

    [Fact]
    public void UserActivityGoals_Create_WithNegativeHydrationGoal_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            UserActivityGoals.Create(stepGoal: null, -1));
    }

    [Theory]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void UserActivityGoals_Create_WithInfiniteHydrationGoal_Throws(double value) {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            UserActivityGoals.Create(stepGoal: null, value));
    }

    [Fact]
    public void UserActivityGoals_With_UpdatesOnlyProvidedValues() {
        var goals = UserActivityGoals.Create(10000, 2.5);

        UserActivityGoals updated = goals.With(stepGoal: 12000);

        Assert.Equal(12000, updated.StepGoal);
        Assert.Equal(2.5, updated.HydrationGoal);
    }

    [Fact]
    public void UserActivityGoals_With_WithNegativeStepGoal_Throws() {
        var goals = UserActivityGoals.Create(10000, 2.5);

        Assert.Throws<ArgumentOutOfRangeException>(() => goals.With(stepGoal: -1));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void UserActivityGoals_With_WithNonFiniteHydration_Throws(double value) {
        var goals = UserActivityGoals.Create(10000, 2.5);

        Assert.Throws<ArgumentOutOfRangeException>(() => goals.With(hydrationGoal: value));
    }

    [Fact]
    public void UserActivityGoals_Create_WithNullValues_Succeeds() {
        var goals = UserActivityGoals.Create(stepGoal: null, hydrationGoal: null);

        Assert.Null(goals.StepGoal);
        Assert.Null(goals.HydrationGoal);
    }

    // --- DietologistPermissions ---

    [Fact]
    public void DietologistPermissions_AllEnabled_AllFieldsTrue() {
        DietologistPermissions perms = DietologistPermissions.AllEnabled;

        Assert.True(perms.ShareMeals);
        Assert.True(perms.ShareStatistics);
        Assert.True(perms.ShareWeight);
        Assert.True(perms.ShareWaist);
        Assert.True(perms.ShareGoals);
        Assert.True(perms.ShareHydration);
        Assert.True(perms.ShareProfile);
        Assert.True(perms.ShareFasting);
    }

    [Fact]
    public void DietologistPermissions_WithSelectiveDisable_PreservesOthers() {
        var perms = new DietologistPermissions(ShareMeals: false, ShareWeight: false);

        Assert.False(perms.ShareMeals);
        Assert.True(perms.ShareStatistics);
        Assert.False(perms.ShareWeight);
        Assert.True(perms.ShareWaist);
        Assert.True(perms.ShareGoals);
        Assert.True(perms.ShareHydration);
        Assert.True(perms.ShareProfile);
        Assert.True(perms.ShareFasting);
    }

    // --- Simple code value objects ---

    [Theory]
    [InlineData(" en ", "en")]
    [InlineData("RU", "ru")]
    public void LanguageCode_TryParse_WithSupportedValues_Normalizes(string value, string expected) {
        bool parsed = LanguageCode.TryParse(value, out LanguageCode language);

        Assert.True(parsed);
        Assert.Equal(expected, language.Value);
        Assert.Equal(expected, language.ToString());
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

    [Theory]
    [InlineData("m", "M")]
    [InlineData(" f ", "F")]
    [InlineData("O", "O")]
    public void GenderCode_TryParse_WithSupportedValues_Normalizes(string value, string expected) {
        bool parsed = GenderCode.TryParse(value, out GenderCode gender);

        Assert.True(parsed);
        Assert.Equal(expected, gender.Value);
        Assert.Equal(expected, gender.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("x")]
    public void GenderCode_TryParse_WithUnsupportedValues_ReturnsFalse(string? value) {
        bool parsed = GenderCode.TryParse(value, out GenderCode gender);

        Assert.False(parsed);
        Assert.Equal(default, gender);
    }

    [Theory]
    [InlineData(" ocean ", "ocean")]
    [InlineData("LEAF", "leaf")]
    [InlineData("dark", "dark")]
    public void ThemeCode_TryParse_WithSupportedValues_Normalizes(string value, string expected) {
        bool parsed = ThemeCode.TryParse(value, out ThemeCode theme);

        Assert.True(parsed);
        Assert.Equal(expected, theme.Value);
        Assert.Equal(expected, theme.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("light")]
    public void ThemeCode_TryParse_WithUnsupportedValues_ReturnsFalse(string? value) {
        bool parsed = ThemeCode.TryParse(value, out ThemeCode theme);

        Assert.False(parsed);
        Assert.Equal(default, theme);
        Assert.Equal("ocean", ThemeCode.Default.Value);
    }

    [Theory]
    [InlineData(" classic ", "classic")]
    [InlineData("MODERN", "modern")]
    public void UiStyleCode_TryParse_WithSupportedValues_Normalizes(string value, string expected) {
        bool parsed = UiStyleCode.TryParse(value, out UiStyleCode style);

        Assert.True(parsed);
        Assert.Equal(expected, style.Value);
        Assert.Equal(expected, style.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("compact")]
    public void UiStyleCode_TryParse_WithUnsupportedValues_ReturnsFalse(string? value) {
        bool parsed = UiStyleCode.TryParse(value, out UiStyleCode style);

        Assert.False(parsed);
        Assert.Equal(default, style);
        Assert.Equal("classic", UiStyleCode.Default.Value);
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
        var weight = DesiredWeight.Create(75.5);
        var waist = DesiredWaist.Create(82.3);

        Assert.Equal(75.5, weight.Value);
        Assert.Equal(82.3, waist.Value);
    }
}
