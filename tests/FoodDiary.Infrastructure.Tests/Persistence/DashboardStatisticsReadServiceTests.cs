using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Dashboard.Models;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Dashboard;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace FoodDiary.Infrastructure.Tests.Persistence;

[ExcludeFromCodeCoverage]
public sealed class DashboardStatisticsReadServiceTests {
    [Fact]
    public async Task GetStatisticsAsync_ProjectsMealNutritionIntoBuckets() {
        await using FoodDiaryDbContext context = CreateContext();
        var user = User.Create($"dashboard-statistics-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);

        Meal firstMeal = CreateMeal(user.Id, new DateTime(2026, 6, 1, 8, 0, 0, DateTimeKind.Utc), calories: 500, proteins: 30);
        Meal secondMeal = CreateMeal(user.Id, new DateTime(2026, 6, 1, 18, 0, 0, DateTimeKind.Utc), calories: 700, proteins: 40);
        Meal thirdMeal = CreateMeal(user.Id, new DateTime(2026, 6, 2, 12, 0, 0, DateTimeKind.Utc), calories: 300, proteins: 15);
        context.Meals.AddRange(firstMeal, secondMeal, thirdMeal);
        await context.SaveChangesAsync();

        var readService = new DashboardStatisticsReadService(context);

        Result<IReadOnlyList<DashboardStatisticsBucketReadModel>> result = await readService.GetStatisticsAsync(
            user.Id,
            new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 6, 2, 23, 59, 59, DateTimeKind.Utc),
            quantizationDays: 1,
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Message);
        Assert.Collection(
            result.Value,
            first => {
                Assert.Equal(new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc), first.DateFrom);
                Assert.Equal(1200, first.TotalCalories);
                Assert.Equal(70, first.AverageProteins);
                Assert.Equal(70, first.TotalProteins);
            },
            second => {
                Assert.Equal(new DateTime(2026, 6, 2, 0, 0, 0, DateTimeKind.Utc), second.DateFrom);
                Assert.Equal(300, second.TotalCalories);
                Assert.Equal(15, second.AverageProteins);
                Assert.Equal(15, second.TotalProteins);
            });
    }

    [Fact]
    public async Task GetStatisticsAsync_WhenDateRangeIsInvalid_ReturnsValidationFailure() {
        await using FoodDiaryDbContext context = CreateContext();
        var readService = new DashboardStatisticsReadService(context);

        Result<IReadOnlyList<DashboardStatisticsBucketReadModel>> result = await readService.GetStatisticsAsync(
            Domain.ValueObjects.Ids.UserId.New(),
            new DateTime(2026, 6, 3, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 6, 2, 0, 0, 0, DateTimeKind.Utc),
            quantizationDays: 1,
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
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
        MethodInfo method = typeof(DashboardStatisticsReadService).GetMethod(
            "NormalizeUtcInstant",
            BindingFlags.Static | BindingFlags.NonPublic)!;
        return (DateTime)method.Invoke(null, [value])!;
    }

    private static Meal CreateMeal(
        Domain.ValueObjects.Ids.UserId userId,
        DateTime date,
        double calories,
        double proteins) {
        var meal = Meal.Create(userId, date);
        meal.ApplyNutrition(new MealNutritionUpdate(
            calories,
            proteins,
            TotalFats: 10,
            TotalCarbs: 20,
            TotalFiber: 5,
            TotalAlcohol: 0,
            IsAutoCalculated: false,
            ManualCalories: calories,
            ManualProteins: proteins,
            ManualFats: 10,
            ManualCarbs: 20,
            ManualFiber: 5,
            ManualAlcohol: 0));
        return meal;
    }
}
