using FoodDiary.Modules.Meals.Domain.Contracts.Enums;
using FoodDiary.Modules.Meals.Domain.Entities;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace FoodDiary.Infrastructure.Tests.Persistence;

[ExcludeFromCodeCoverage]
public sealed class FoodDiaryDbContextTests {
    [Fact]
    public void CoordinationFlagSharesSessionState() {
        using FoodDiaryDbContext context = CreateContext();
        context.IsCoordinatingModuleSave = true;
        Assert.True(context.Session.IsSaving);
        context.IsCoordinatingModuleSave = false;
        Assert.False(context.Session.IsSaving);
    }

    [Fact]
    public void DbSetProperties_ReturnEntitySets() {
        using FoodDiaryDbContext context = CreateContext();

        Assert.NotNull(context.MealAiItems);
        Assert.NotNull(context.RecipeIngredients);
        Assert.NotNull(context.ShoppingListItemSources);
        Assert.NotNull(context.ExerciseEntries);
        Assert.NotNull(context.WeightGoals);
        Assert.NotNull(context.WaistGoals);
        Assert.NotNull(context.NutritionLessons);
        Assert.NotNull(context.UserLessonProgress);
        Assert.NotNull(context.MealPlans);
        Assert.NotNull(context.MealPlanDays);
        Assert.NotNull(context.MealPlanMeals);
        Assert.Same(context.Set<FoodDiary.Modules.Ai.PersistenceModel.AiQuotaPeriod>(), context.AiQuotaPeriods);
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
