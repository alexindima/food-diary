using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Domain.Tests.Domain.ValueObjects;

[ExcludeFromCodeCoverage]
public class AdditionalValueObjectsInvariantTests {
    // --- MealAiItemState ---

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

}
