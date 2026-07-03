using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Dashboard.Models;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.Entities.FavoriteMeals;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Dashboard;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace FoodDiary.Infrastructure.Tests.Persistence;

[ExcludeFromCodeCoverage]
public sealed class DashboardMealsReadServiceTests {
    [Fact]
    public async Task GetMealsAsync_ProjectsMealsWithItemsFavoritesAndAiSessions() {
        await using FoodDiaryDbContext context = CreateContext();
        var user = User.Create($"dashboard-meals-{Guid.NewGuid():N}@example.com", "hash");
        var product = Product.Create(
            user.Id,
            "Rice",
            MeasurementUnit.G,
            baseAmount: 100,
            defaultPortionAmount: 100,
            caloriesPerBase: 130,
            proteinsPerBase: 2.7,
            fatsPerBase: 0.3,
            carbsPerBase: 28,
            fiberPerBase: 0.4,
            alcoholPerBase: 0,
            imageUrl: "https://cdn.example.com/rice.webp");
        var aiAsset = ImageAsset.Create(user.Id, "meals/ai.webp", "https://cdn.example.com/ai.webp");
        Meal meal = CreateMeal(user.Id);
        meal.AddProduct(product.Id, 150);
        meal.AddAiSession(
            aiAsset.Id,
            AiRecognitionSource.Photo,
            new DateTime(2026, 6, 1, 12, 5, 0, DateTimeKind.Utc),
            "AI plate",
            [MealAiItemData.Create("Rice", "Local rice", 150, "g", 195, 4, 0.5, 42, 0.6, 0)]);
        context.AddRange(user, product, aiAsset, meal, FavoriteMeal.Create(user.Id, meal.Id, "Lunch"));
        await context.SaveChangesAsync();

        var readService = new DashboardMealsReadService(context);

        Result<DashboardMealsReadModel> result = await readService.GetMealsAsync(
            user.Id,
            page: 1,
            limit: 10,
            new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 6, 1, 23, 59, 59, DateTimeKind.Utc),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Message);
        DashboardMealReadModel projectedMeal = Assert.Single(result.Value.Items);
        Assert.Equal(meal.Id.Value, projectedMeal.Id);
        Assert.Equal("Lunch", projectedMeal.MealType);
        Assert.True(projectedMeal.IsFavorite);
        Assert.NotNull(projectedMeal.FavoriteMealId);
        DashboardMealItemReadModel projectedItem = Assert.Single(projectedMeal.Items);
        Assert.Equal(product.Id.Value, projectedItem.ProductId);
        Assert.Equal("Rice", projectedItem.ProductName);
        Assert.Equal("https://cdn.example.com/rice.webp", projectedItem.ProductImageUrl);
        Assert.NotNull(projectedItem.ProductQualityScore);
        DashboardMealAiSessionReadModel projectedSession = Assert.Single(projectedMeal.AiSessions);
        Assert.Equal(aiAsset.Id.Value, projectedSession.ImageAssetId);
        Assert.Equal("https://cdn.example.com/ai.webp", projectedSession.ImageUrl);
        Assert.Equal("Photo", projectedSession.Source);
        DashboardMealAiItemReadModel projectedAiItem = Assert.Single(projectedSession.Items);
        Assert.Equal("Rice", projectedAiItem.NameEn);
        Assert.Equal("Local rice", projectedAiItem.NameLocal);
        Assert.Equal(150, projectedAiItem.Amount);
    }

    [Fact]
    public async Task GetMealsAsync_WhenDateRangeIsInvalid_ReturnsValidationFailure() {
        await using FoodDiaryDbContext context = CreateContext();
        var readService = new DashboardMealsReadService(context);

        Result<DashboardMealsReadModel> result = await readService.GetMealsAsync(
            Domain.ValueObjects.Ids.UserId.New(),
            page: 1,
            limit: 10,
            new DateTime(2026, 6, 2, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public async Task GetMealsAsync_WithMealWithoutAiSessions_ReturnsEmptyAiSessions() {
        await using FoodDiaryDbContext context = CreateContext();
        var user = User.Create($"dashboard-meals-empty-ai-{Guid.NewGuid():N}@example.com", "hash");
        Meal meal = CreateMeal(user.Id);
        context.AddRange(user, meal);
        await context.SaveChangesAsync();
        var readService = new DashboardMealsReadService(context);

        Result<DashboardMealsReadModel> result = await readService.GetMealsAsync(
            user.Id,
            page: 1,
            limit: 10,
            new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 6, 1, 23, 59, 59, DateTimeKind.Utc),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Message);
        DashboardMealReadModel projectedMeal = Assert.Single(result.Value.Items);
        Assert.Empty(projectedMeal.AiSessions);
    }

    [Theory]
    [InlineData(DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void NormalizeUtcInstant_HandlesAllDateTimeKinds(DateTimeKind kind) {
        DateTime value = new(2026, 6, 1, 12, 0, 0, kind);

        DateTime normalized = InvokeNormalizeUtcInstant(value);

        Assert.Equal(DateTimeKind.Utc, normalized.Kind);
    }

    private static FoodDiaryDbContext CreateContext() {
        DbContextOptions<FoodDiaryDbContext> options = new DbContextOptionsBuilder<FoodDiaryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new FoodDiaryDbContext(options);
    }

    private static DateTime InvokeNormalizeUtcInstant(DateTime value) {
        MethodInfo method = typeof(DashboardMealsReadService).GetMethod(
            "NormalizeUtcInstant",
            BindingFlags.Static | BindingFlags.NonPublic)!;
        return (DateTime)method.Invoke(null, [value])!;
    }

    private static Meal CreateMeal(Domain.ValueObjects.Ids.UserId userId) {
        var meal = Meal.Create(
            userId,
            new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc),
            MealType.Lunch,
            "Lunch",
            imageUrl: null,
            imageAssetId: null,
            preMealSatietyLevel: 2,
            postMealSatietyLevel: 4);
        meal.ApplyNutrition(new MealNutritionUpdate(
            TotalCalories: 195,
            TotalProteins: 4,
            TotalFats: 0.5,
            TotalCarbs: 42,
            TotalFiber: 0.6,
            TotalAlcohol: 0,
            IsAutoCalculated: true,
            ManualCalories: null,
            ManualProteins: null,
            ManualFats: null,
            ManualCarbs: null,
            ManualFiber: null,
            ManualAlcohol: null));
        return meal;
    }
}
