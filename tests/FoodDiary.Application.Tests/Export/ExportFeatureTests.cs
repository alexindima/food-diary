using FoodDiary.Application.Export.Common;
using FoodDiary.Application.Export.Models;
using FoodDiary.Application.Export.Queries.ExportDiary;
using FoodDiary.Application.Export.Services;
using FoodDiary.Application.Meals.Common;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Export;

public class ExportFeatureTests {
    private static readonly DateTime TestDate = new(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);

    private static Meal CreateMeal(
        UserId? userId = null,
        DateTime? date = null,
        MealType? mealType = MealType.Breakfast,
        string? comment = null) {
        var meal = Meal.Create(userId ?? UserId.New(), date ?? TestDate, mealType, comment);
        meal.ApplyNutrition(new MealNutritionUpdate(500, 30, 20, 60, 5, 0, IsAutoCalculated: true));
        return meal;
    }

    private static ExportDiaryQueryHandler CreateHandler(IReadOnlyList<Meal> meals) =>
        new(new StubMealRepository(meals), new StubPdfGenerator());

    [Fact]
    public async Task ExportDiary_WithMeals_ReturnsCsvFileResult() {
        var userId = UserId.New();
        var meals = new[] { CreateMeal(userId), CreateMeal(userId, TestDate.AddDays(1), MealType.Lunch) };
        var handler = CreateHandler(meals);

        var result = await handler.Handle(
            new ExportDiaryQuery(userId.Value, TestDate, TestDate.AddDays(1)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("text/csv", result.Value.ContentType);
        Assert.Contains("food-diary-", result.Value.FileName);
        Assert.EndsWith(".csv", result.Value.FileName);
        Assert.True(result.Value.Content.Length > 0);
    }

    [Fact]
    public async Task ExportDiary_WithPdfFormat_ReturnsPdfFileResult() {
        var userId = UserId.New();
        var meals = new[] { CreateMeal(userId) };
        var handler = CreateHandler(meals);

        var result = await handler.Handle(
            new ExportDiaryQuery(userId.Value, TestDate, TestDate.AddDays(1), ExportFormat.Pdf),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("application/pdf", result.Value.ContentType);
        Assert.EndsWith(".pdf", result.Value.FileName);
    }

    [Fact]
    public async Task ExportDiary_WithNoMeals_ReturnsHeaderOnly() {
        var userId = UserId.New();
        var handler = CreateHandler([]);

        var result = await handler.Handle(
            new ExportDiaryQuery(userId.Value, TestDate, TestDate.AddDays(1)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var content = System.Text.Encoding.UTF8.GetString(result.Value.Content);
        Assert.Contains("Date,MealType,Calories", content);
    }

    [Fact]
    public async Task ExportDiary_WithNullUserId_ReturnsFailure() {
        var handler = CreateHandler([]);

        var result = await handler.Handle(
            new ExportDiaryQuery(null, TestDate, TestDate.AddDays(1)),
            CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void CsvGenerator_WithAutoCalculated_UsesTotals() {
        var meal = CreateMeal();

        var csv = DiaryCsvGenerator.Generate([meal]);
        var content = System.Text.Encoding.UTF8.GetString(csv);

        Assert.Contains("500", content);
        Assert.Contains("30", content);
        Assert.Contains("Breakfast", content);
    }

    [Fact]
    public void CsvGenerator_WithManualOverride_UsesManualValues() {
        var meal = CreateMeal();
        meal.ApplyNutrition(new MealNutritionUpdate(
            500, 30, 20, 60, 5, 0,
            IsAutoCalculated: false,
            ManualCalories: 400, ManualProteins: 25, ManualFats: 15,
            ManualCarbs: 50, ManualFiber: 3, ManualAlcohol: 0));

        var csv = DiaryCsvGenerator.Generate([meal]);
        var content = System.Text.Encoding.UTF8.GetString(csv);

        Assert.Contains("400", content);
        Assert.Contains("25", content);
    }

    [Fact]
    public void CsvGenerator_WithCommaInComment_EscapesProperly() {
        var meal = CreateMeal(comment: "eggs, bacon");

        var csv = DiaryCsvGenerator.Generate([meal]);
        var content = System.Text.Encoding.UTF8.GetString(csv);

        Assert.Contains("\"eggs, bacon\"", content);
    }

    [Fact]
    public void CsvGenerator_WithQuoteInComment_EscapesProperly() {
        var meal = CreateMeal(comment: "she said \"hello\"");

        var csv = DiaryCsvGenerator.Generate([meal]);
        var content = System.Text.Encoding.UTF8.GetString(csv);

        Assert.Contains("\"she said \"\"hello\"\"\"", content);
    }

    [Fact]
    public void CsvGenerator_WithNoMealType_WritesEmptyField() {
        var meal = CreateMeal(mealType: null);

        var csv = DiaryCsvGenerator.Generate([meal]);
        var lines = System.Text.Encoding.UTF8.GetString(csv).Split('\n', StringSplitOptions.RemoveEmptyEntries);

        Assert.True(lines.Length >= 2);
        var dataLine = lines[1];
        Assert.Contains(",,", dataLine);
    }

    [Fact]
    public void CsvGenerator_HasUtf8Bom() {
        var csv = DiaryCsvGenerator.Generate([]);

        Assert.True(csv.Length >= 3);
        Assert.Equal(0xEF, csv[0]);
        Assert.Equal(0xBB, csv[1]);
        Assert.Equal(0xBF, csv[2]);
    }

    private sealed class StubMealRepository(IReadOnlyList<Meal> meals) : IMealRepository {
        public Task<IReadOnlyList<Meal>> GetByPeriodAsync(
            UserId userId, DateTime dateFrom, DateTime dateTo, CancellationToken ct = default) =>
            Task.FromResult(meals);

        public Task<Meal> AddAsync(Meal meal, CancellationToken ct = default) => throw new NotSupportedException();
        public Task UpdateAsync(Meal meal, CancellationToken ct = default) => throw new NotSupportedException();
        public Task DeleteAsync(Meal meal, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Meal?> GetByIdAsync(MealId id, UserId userId, bool includeItems = false, bool asTracking = false, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<Meal> Items, int TotalItems)> GetPagedAsync(UserId userId, int page, int limit, DateTime? dateFrom, DateTime? dateTo, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<DateTime>> GetDistinctMealDatesAsync(UserId userId, DateTime dateFrom, DateTime dateTo, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<int> GetTotalMealCountAsync(UserId userId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Meal>> GetWithItemsAndProductsAsync(UserId userId, DateTime date, CancellationToken ct = default) => throw new NotSupportedException();
    }

    private sealed class StubPdfGenerator : IDiaryPdfGenerator {
        public byte[] Generate(IReadOnlyList<Meal> meals, DateTime dateFrom, DateTime dateTo) =>
            [0x25, 0x50, 0x44, 0x46]; // %PDF magic bytes
    }
}
