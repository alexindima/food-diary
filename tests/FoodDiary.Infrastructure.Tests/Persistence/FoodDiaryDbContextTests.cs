using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Enums;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FoodDiary.Infrastructure.Tests.Persistence;

[ExcludeFromCodeCoverage]
public sealed class FoodDiaryDbContextTests {
    [Fact]
    public void DbSetProperties_ReturnEntitySets() {
        using FoodDiaryDbContext context = CreateContext();

        Assert.NotNull(context.MealAiItems);
        Assert.NotNull(context.RecipeIngredients);
        Assert.NotNull(context.ShoppingListItemSources);
        Assert.NotNull(context.ExerciseEntries);
        Assert.NotNull(context.NutritionLessons);
        Assert.NotNull(context.UserLessonProgress);
        Assert.NotNull(context.MealPlans);
        Assert.NotNull(context.MealPlanDays);
        Assert.NotNull(context.MealPlanMeals);
    }

    [Fact]
    public void MealAiItemResolution_UsesExplicitSentinelForDatabaseDefault() {
        using FoodDiaryDbContext context = CreateContext();

        IEntityType entityType = context.Model.FindEntityType(typeof(MealAiItem))!;
        IProperty property = entityType.FindProperty(nameof(MealAiItem.Resolution))!;

        Assert.Equal(MealAiItemResolution.Accepted, property.GetDefaultValue());
        Assert.Equal((MealAiItemResolution)0, property.Sentinel);
    }

    private static FoodDiaryDbContext CreateContext() {
        DbContextOptions<FoodDiaryDbContext> options = new DbContextOptionsBuilder<FoodDiaryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new FoodDiaryDbContext(options);
    }
}
