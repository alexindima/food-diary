using FoodDiary.Domain.Entities.MealPlans;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Domain;

public class MealPlanInvariantTests {
    [Fact]
    public void CreateCurated_WithBlankName_Throws() {
        Assert.Throws<ArgumentException>(() =>
            MealPlan.CreateCurated("   ", null, DietType.Balanced, 7, null));
    }

    [Fact]
    public void CreateCurated_WithTooLongName_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            MealPlan.CreateCurated(new string('n', 257), null, DietType.Balanced, 7, null));
    }

    [Fact]
    public void CreateCurated_WithTooLongDescription_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            MealPlan.CreateCurated("Plan", new string('d', 2049), DietType.Balanced, 7, null));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(32)]
    public void CreateCurated_WithInvalidDuration_Throws(int duration) {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            MealPlan.CreateCurated("Plan", null, DietType.Balanced, duration, null));
    }

    [Fact]
    public void CreateCurated_NormalizesNameAndDescription() {
        var plan = MealPlan.CreateCurated("  Weekly Plan  ", "  Healthy eating  ", DietType.Balanced, 7, 2000);

        Assert.Equal("Weekly Plan", plan.Name);
        Assert.Equal("Healthy eating", plan.Description);
        Assert.True(plan.IsCurated);
        Assert.Null(plan.UserId);
        Assert.Equal(7, plan.DurationDays);
        Assert.Equal(2000, plan.TargetCaloriesPerDay);
    }

    [Fact]
    public void CreateCurated_WithWhitespaceDescription_SetsNull() {
        var plan = MealPlan.CreateCurated("Plan", "   ", DietType.Balanced, 7, null);

        Assert.Null(plan.Description);
    }

    [Fact]
    public void CreateForUser_WithEmptyUserId_Throws() {
        Assert.Throws<ArgumentException>(() =>
            MealPlan.CreateForUser(UserId.Empty, "Plan", null, DietType.Balanced, 7, null));
    }

    [Fact]
    public void CreateForUser_SetsUserIdAndNotCurated() {
        var userId = UserId.New();
        var plan = MealPlan.CreateForUser(userId, "My Plan", null, DietType.Balanced, 7, null);

        Assert.Equal(userId, plan.UserId);
        Assert.False(plan.IsCurated);
    }

    [Fact]
    public void AddDay_ReturnsDayWithCorrectNumber() {
        var plan = MealPlan.CreateCurated("Plan", null, DietType.Balanced, 7, null);

        var day = plan.AddDay(1);

        Assert.Equal(1, day.DayNumber);
        Assert.Single(plan.Days);
    }

    [Fact]
    public void AddDay_WithDuplicateDayNumber_Throws() {
        var plan = MealPlan.CreateCurated("Plan", null, DietType.Balanced, 7, null);
        plan.AddDay(1);

        Assert.Throws<InvalidOperationException>(() => plan.AddDay(1));
    }

    [Fact]
    public void AddDay_MealPlanDay_AddMeal_WithZeroServings_Throws() {
        var plan = MealPlan.CreateCurated("Plan", null, DietType.Balanced, 7, null);
        var day = plan.AddDay(1);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            day.AddMeal(MealType.Breakfast, RecipeId.New(), servings: 0));
    }

    [Fact]
    public void AddDay_MealPlanDay_AddMeal_WithNegativeServings_Throws() {
        var plan = MealPlan.CreateCurated("Plan", null, DietType.Balanced, 7, null);
        var day = plan.AddDay(1);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            day.AddMeal(MealType.Breakfast, RecipeId.New(), servings: -1));
    }

    [Fact]
    public void AddDay_MealPlanDay_AddMeal_Succeeds() {
        var plan = MealPlan.CreateCurated("Plan", null, DietType.Balanced, 7, null);
        var day = plan.AddDay(1);
        var recipeId = RecipeId.New();

        var meal = day.AddMeal(MealType.Lunch, recipeId, 2);

        Assert.Equal(MealType.Lunch, meal.MealType);
        Assert.Equal(recipeId, meal.RecipeId);
        Assert.Equal(2, meal.Servings);
        Assert.Single(day.Meals);
    }

    [Fact]
    public void Adopt_WithEmptyUserId_Throws() {
        var plan = MealPlan.CreateCurated("Plan", null, DietType.Balanced, 7, null);

        Assert.Throws<ArgumentException>(() => plan.Adopt(UserId.Empty));
    }

    [Fact]
    public void Adopt_CopiesPlanWithDaysAndMeals() {
        var plan = MealPlan.CreateCurated("Plan", "Desc", DietType.LowCarb, 3, 1800);
        var day1 = plan.AddDay(1);
        day1.AddMeal(MealType.Breakfast, RecipeId.New(), 1);
        day1.AddMeal(MealType.Lunch, RecipeId.New(), 2);
        var day2 = plan.AddDay(2);
        day2.AddMeal(MealType.Dinner, RecipeId.New(), 1);
        var userId = UserId.New();

        var adopted = plan.Adopt(userId);

        Assert.NotEqual(plan.Id, adopted.Id);
        Assert.Equal(userId, adopted.UserId);
        Assert.False(adopted.IsCurated);
        Assert.Equal("Plan", adopted.Name);
        Assert.Equal("Desc", adopted.Description);
        Assert.Equal(DietType.LowCarb, adopted.DietType);
        Assert.Equal(3, adopted.DurationDays);
        Assert.Equal(1800, adopted.TargetCaloriesPerDay);
        Assert.Equal(2, adopted.Days.Count);

        var adoptedDay1 = adopted.Days.First(d => d.DayNumber == 1);
        Assert.Equal(2, adoptedDay1.Meals.Count);
    }
}
