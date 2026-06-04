using FoodDiary.Application.Abstractions.Export.Common;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Export.Models;
using FoodDiary.Application.Export.Queries.ExportDiary;
using FoodDiary.Application.Export.Services;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Export;

[ExcludeFromCodeCoverage]
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
        CreateHandler(new StubMealRepository(meals));

    private static ExportDiaryQueryHandler CreateHandler(StubMealRepository repository) =>
        new(repository, new SingleUserRepository(), new StubPdfGenerator());

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
        Assert.Contains("food-diary-", result.Value.FileName, StringComparison.Ordinal);
        Assert.EndsWith(".csv", result.Value.FileName, StringComparison.Ordinal);
        Assert.True(result.Value.Content.Length > 0);
    }

    [Fact]
    public async Task ExportDiary_WithPdfFormat_ReturnsPdfFileResult() {
        var userId = UserId.New();
        var meals = new[] { CreateMeal(userId) };
        var pdfGenerator = new StubPdfGenerator();
        var handler = new ExportDiaryQueryHandler(new StubMealRepository(meals), new SingleUserRepository(), pdfGenerator);

        var result = await handler.Handle(
            new ExportDiaryQuery(userId.Value, TestDate, TestDate.AddDays(1), ExportFormat.Pdf, "ru", 240, "https://Ð´Ð½ÐµÐ²Ð½Ð¸ÐºÐµÐ´Ñ‹.Ñ€Ñ„"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("application/pdf", result.Value.ContentType);
        Assert.EndsWith(".pdf", result.Value.FileName, StringComparison.Ordinal);
        Assert.Equal("ru", pdfGenerator.LastLocale);
        Assert.Equal(240, pdfGenerator.LastTimeZoneOffsetMinutes);
        Assert.Equal("https://Ð´Ð½ÐµÐ²Ð½Ð¸ÐºÐµÐ´Ñ‹.Ñ€Ñ„", pdfGenerator.LastReportOrigin);
    }

    [Fact]
    public async Task ExportDiary_WithUnsafeReportOrigin_DropsOriginBeforePdfGeneration() {
        var userId = UserId.New();
        var pdfGenerator = new StubPdfGenerator();
        var handler = new ExportDiaryQueryHandler(new StubMealRepository([]), new SingleUserRepository(), pdfGenerator);

        var result = await handler.Handle(
            new ExportDiaryQuery(userId.Value, TestDate, TestDate.AddDays(1), ExportFormat.Pdf, ReportOrigin: "javascript:alert(1)"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(pdfGenerator.LastReportOrigin);
    }

    [Fact]
    public async Task ExportDiary_WithLocalDayUtcBoundaries_PreservesRequestedInstants() {
        var userId = UserId.New();
        var repository = new StubMealRepository([]);
        var handler = CreateHandler(repository);
        var localDayStartUtc = new DateTimeOffset(2026, 5, 4, 0, 0, 0, TimeSpan.FromHours(4)).UtcDateTime;
        var localDayEndUtc = new DateTimeOffset(2026, 5, 4, 23, 59, 59, 999, TimeSpan.FromHours(4)).UtcDateTime;

        var result = await handler.Handle(
            new ExportDiaryQuery(userId.Value, localDayStartUtc, localDayEndUtc, ExportFormat.Pdf),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(localDayStartUtc, repository.LastDateFrom);
        Assert.Equal(localDayEndUtc, repository.LastDateTo);
    }

    [Fact]
    public async Task ExportDiary_WithRepositoryOverfetch_FiltersToRequestedInstants() {
        var userId = UserId.New();
        var localDayStartUtc = new DateTimeOffset(2026, 5, 4, 0, 0, 0, TimeSpan.FromHours(4)).UtcDateTime;
        var localDayEndUtc = new DateTimeOffset(2026, 5, 4, 23, 59, 59, 999, TimeSpan.FromHours(4)).UtcDateTime;
        var beforeMeal = CreateMeal(userId, localDayStartUtc.AddMinutes(-1), comment: "outside before");
        var includedMeal = CreateMeal(userId, localDayStartUtc.AddMinutes(1), comment: "inside period");
        var afterMeal = CreateMeal(userId, localDayEndUtc.AddMilliseconds(1), comment: "outside after");
        var handler = CreateHandler([beforeMeal, includedMeal, afterMeal]);

        var result = await handler.Handle(
            new ExportDiaryQuery(userId.Value, localDayStartUtc, localDayEndUtc),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        var content = System.Text.Encoding.UTF8.GetString(result.Value.Content);
        Assert.Contains("inside period", content, StringComparison.Ordinal);
        Assert.DoesNotContain("outside before", content, StringComparison.Ordinal);
        Assert.DoesNotContain("outside after", content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExportDiary_WithCsvFormat_UsesLocalDatesForFilenameAndRows() {
        var userId = UserId.New();
        var localDayStartUtc = new DateTimeOffset(2026, 5, 4, 0, 0, 0, TimeSpan.FromHours(4)).UtcDateTime;
        var localDayEndUtc = new DateTimeOffset(2026, 5, 4, 23, 59, 59, 999, TimeSpan.FromHours(4)).UtcDateTime;
        var meal = CreateMeal(userId, localDayStartUtc.AddHours(1), comment: "local day meal");
        var handler = CreateHandler([meal]);

        var result = await handler.Handle(
            new ExportDiaryQuery(userId.Value, localDayStartUtc, localDayEndUtc, ExportFormat.Csv, TimeZoneOffsetMinutes: 240),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Contains("food-diary-2026-05-04-to-2026-05-04.csv", result.Value.FileName, StringComparison.Ordinal);
        var content = System.Text.Encoding.UTF8.GetString(result.Value.Content);
        Assert.Contains("2026-05-04,Breakfast", content, StringComparison.Ordinal);
        Assert.Contains("local day meal", content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExportDiary_WithMissingTimeZoneOffset_InfersDisplayDateFromRangeStart() {
        var userId = UserId.New();
        var localDayStartUtc = new DateTimeOffset(2026, 5, 4, 0, 0, 0, TimeSpan.FromHours(4)).UtcDateTime;
        var localDayEndUtc = new DateTimeOffset(2026, 5, 4, 23, 59, 59, 999, TimeSpan.FromHours(4)).UtcDateTime;
        var meal = CreateMeal(userId, localDayStartUtc.AddHours(1));
        var handler = CreateHandler([meal]);

        var result = await handler.Handle(
            new ExportDiaryQuery(userId.Value, localDayStartUtc, localDayEndUtc),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Contains("food-diary-2026-05-04-to-2026-05-04.csv", result.Value.FileName, StringComparison.Ordinal);
        var content = System.Text.Encoding.UTF8.GetString(result.Value.Content);
        Assert.Contains("2026-05-04,Breakfast", content, StringComparison.Ordinal);
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
        Assert.Contains("Date,MealType,Calories", content, StringComparison.Ordinal);
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
    public async Task ExportDiary_WithDateFromAfterDateTo_ReturnsValidationFailure() {
        var userId = UserId.New();
        var handler = CreateHandler([]);

        var result = await handler.Handle(
            new ExportDiaryQuery(userId.Value, TestDate.AddDays(1), TestDate),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public async Task ExportDiary_WithRangeOverOneYear_ReturnsValidationFailure() {
        var userId = UserId.New();
        var handler = CreateHandler([]);

        var result = await handler.Handle(
            new ExportDiaryQuery(userId.Value, TestDate, TestDate.AddDays(367)),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public async Task ExportDiary_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("export-deleted@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new ExportDiaryQueryHandler(
            new StubMealRepository([]),
            new SingleUserRepository(user),
            new StubPdfGenerator());

        var result = await handler.Handle(
            new ExportDiaryQuery(user.Id.Value, TestDate, TestDate.AddDays(1)),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public void CsvGenerator_WithAutoCalculated_UsesTotals() {
        var meal = CreateMeal();

        var csv = DiaryCsvGenerator.Generate([meal]);
        var content = System.Text.Encoding.UTF8.GetString(csv);

        Assert.Contains("500", content, StringComparison.Ordinal);
        Assert.Contains("30", content, StringComparison.Ordinal);
        Assert.Contains("Breakfast", content, StringComparison.Ordinal);
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

        Assert.Contains("400", content, StringComparison.Ordinal);
        Assert.Contains("25", content, StringComparison.Ordinal);
    }

    [Fact]
    public void CsvGenerator_WithCommaInComment_EscapesProperly() {
        var meal = CreateMeal(comment: "eggs, bacon");

        var csv = DiaryCsvGenerator.Generate([meal]);
        var content = System.Text.Encoding.UTF8.GetString(csv);

        Assert.Contains("\"eggs, bacon\"", content, StringComparison.Ordinal);
    }

    [Fact]
    public void CsvGenerator_WithQuoteInComment_EscapesProperly() {
        var meal = CreateMeal(comment: "she said \"hello\"");

        var csv = DiaryCsvGenerator.Generate([meal]);
        var content = System.Text.Encoding.UTF8.GetString(csv);

        Assert.Contains("\"she said \"\"hello\"\"\"", content, StringComparison.Ordinal);
    }

    [Fact]
    public void CsvGenerator_WithNewlineInComment_EscapesProperly() {
        var meal = CreateMeal(comment: "first line\nsecond line");

        var csv = DiaryCsvGenerator.Generate([meal]);
        var content = System.Text.Encoding.UTF8.GetString(csv);

        Assert.Contains("\"first line\nsecond line\"", content, StringComparison.Ordinal);
    }

    [Fact]
    public void CsvGenerator_WithNoMealType_WritesEmptyField() {
        var meal = CreateMeal(mealType: null);

        var csv = DiaryCsvGenerator.Generate([meal]);
        var lines = System.Text.Encoding.UTF8.GetString(csv).Split('\n', StringSplitOptions.RemoveEmptyEntries);

        Assert.True(lines.Length >= 2);
        var dataLine = lines[1];
        Assert.Contains(",,", dataLine, StringComparison.Ordinal);
    }

    [Fact]
    public void CsvGenerator_WithTimeZoneOffset_WritesDisplayDate() {
        var meal = CreateMeal(date: new DateTime(2026, 5, 3, 21, 0, 0, DateTimeKind.Utc));

        var csv = DiaryCsvGenerator.Generate([meal], timeZoneOffsetMinutes: 240);
        var content = System.Text.Encoding.UTF8.GetString(csv);

        Assert.Contains("2026-05-04,Breakfast", content, StringComparison.Ordinal);
    }

    [Fact]
    public void CsvGenerator_WithInvalidTimeZoneOffset_FallsBackToUtc() {
        var meal = CreateMeal(date: new DateTime(2026, 5, 3, 21, 0, 0, DateTimeKind.Utc));

        var csv = DiaryCsvGenerator.Generate([meal], timeZoneOffsetMinutes: 900);
        var content = System.Text.Encoding.UTF8.GetString(csv);

        Assert.Contains("2026-05-03,Breakfast", content, StringComparison.Ordinal);
    }

    [Fact]
    public void CsvGenerator_WithUnspecifiedDate_TreatsValueAsUtc() {
        var meal = CreateMeal();
        SetMealDate(meal, new DateTime(2026, 5, 3, 21, 0, 0, DateTimeKind.Unspecified));

        var csv = DiaryCsvGenerator.Generate([meal], TimeSpan.FromHours(4));
        var content = System.Text.Encoding.UTF8.GetString(csv);

        Assert.Contains("2026-05-04,Breakfast", content, StringComparison.Ordinal);
    }

    [Fact]
    public void CsvGenerator_WithLocalDate_ConvertsValueToUtcBeforeApplyingOffset() {
        var localDate = DateTime.SpecifyKind(new DateTime(2026, 5, 3, 21, 0, 0), DateTimeKind.Local);
        var meal = CreateMeal();
        SetMealDate(meal, localDate);

        var csv = DiaryCsvGenerator.Generate([meal], TimeSpan.Zero);
        var content = System.Text.Encoding.UTF8.GetString(csv);
        var expectedDate = localDate.ToUniversalTime().ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

        Assert.Contains($"{expectedDate},Breakfast", content, StringComparison.Ordinal);
    }

    [Fact]
    public void CsvGenerator_HasUtf8Bom() {
        var csv = DiaryCsvGenerator.Generate([]);

        Assert.True(csv.Length >= 3);
        Assert.Equal(0xEF, csv[0]);
        Assert.Equal(0xBB, csv[1]);
        Assert.Equal(0xBF, csv[2]);
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubMealRepository(IReadOnlyList<Meal> meals) : IMealRepository {
        public DateTime? LastDateFrom { get; private set; }
        public DateTime? LastDateTo { get; private set; }

        public Task<IReadOnlyList<Meal>> GetByPeriodAsync(
            UserId userId, DateTime dateFrom, DateTime dateTo, CancellationToken ct = default) {
            LastDateFrom = dateFrom;
            LastDateTo = dateTo;
            return Task.FromResult(meals);
        }

        public Task<Meal> AddAsync(Meal meal, CancellationToken ct = default) => throw new NotSupportedException();
        public Task UpdateAsync(Meal meal, CancellationToken ct = default) => throw new NotSupportedException();
        public Task DeleteAsync(Meal meal, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<Meal?> GetByIdAsync(MealId id, UserId userId, bool includeItems = false, bool asTracking = false, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<Meal> Items, int TotalItems)> GetPagedAsync(UserId userId, int page, int limit, DateTime? dateFrom, DateTime? dateTo, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<DateTime>> GetDistinctMealDatesAsync(UserId userId, DateTime dateFrom, DateTime dateTo, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<int> GetTotalMealCountAsync(UserId userId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Meal>> GetWithItemsAndProductsAsync(UserId userId, DateTime date, CancellationToken ct = default) => throw new NotSupportedException();
    }

    [ExcludeFromCodeCoverage]
    private sealed class SingleUserRepository(User? user = null) : IUserRepository {
        public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<User?> GetByIdAsync(UserId id, CancellationToken ct = default) =>
            Task.FromResult<User?>(user is null || user.Id == id ? user ?? User.Create("export-user@example.com", "hash") : null);
        public Task<User?> GetByIdIncludingDeletedAsync(UserId id, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<User?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<User?> GetByTelegramUserIdIncludingDeletedAsync(long telegramUserId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(string? search, int page, int limit, bool includeDeleted, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers)> GetAdminDashboardSummaryAsync(int recentLimit, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Role>> GetRolesByNamesAsync(IReadOnlyList<string> names, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<User> AddAsync(User user, CancellationToken ct = default) => throw new NotSupportedException();
        public Task UpdateAsync(User user, CancellationToken ct = default) => throw new NotSupportedException();
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubPdfGenerator : IDiaryPdfGenerator {
        public string? LastLocale { get; private set; }
        public int? LastTimeZoneOffsetMinutes { get; private set; }
        public string? LastReportOrigin { get; private set; }

        public Task<byte[]> GenerateAsync(
            IReadOnlyList<Meal> meals,
            DateTime dateFrom,
            DateTime dateTo,
            string? locale,
            int? timeZoneOffsetMinutes,
            string? reportOrigin,
            CancellationToken cancellationToken) {
            LastLocale = locale;
            LastTimeZoneOffsetMinutes = timeZoneOffsetMinutes;
            LastReportOrigin = reportOrigin;
            return Task.FromResult<byte[]>([0x25, 0x50, 0x44, 0x46]); // %PDF magic bytes
        }
    }

    private static void SetMealDate(Meal meal, DateTime date) {
        typeof(Meal)
            .GetProperty(nameof(Meal.Date))!
            .SetValue(meal, date);
    }
}
