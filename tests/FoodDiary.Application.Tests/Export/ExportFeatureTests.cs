using FoodDiary.Application.Abstractions.Export.Common;
using FoodDiary.Application.Abstractions.Cycles.Common;
using FoodDiary.Application.Abstractions.Cycles.Models;
using FoodDiary.Application.Abstractions.Dashboard.Common;
using FoodDiary.Application.Cycles.Services;
using FoodDiary.Application.Export.Models;
using FoodDiary.Application.Export.Queries.ExportCycle;
using FoodDiary.Application.Export.Queries.ExportDiary;
using FoodDiary.Application.Export.Services;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Abstractions.Meals.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;

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

    private static MealConsumptionReadModel ToReadModel(Meal meal) =>
        new(
            meal.Id.Value,
            meal.Date,
            meal.MealType,
            meal.Comment,
            meal.ImageUrl,
            meal.ImageAssetId?.Value,
            meal.TotalCalories,
            meal.TotalProteins,
            meal.TotalFats,
            meal.TotalCarbs,
            meal.TotalFiber,
            meal.TotalAlcohol,
            meal.IsNutritionAutoCalculated,
            meal.ManualCalories,
            meal.ManualProteins,
            meal.ManualFats,
            meal.ManualCarbs,
            meal.ManualFiber,
            meal.ManualAlcohol,
            meal.PreMealSatietyLevel,
            meal.PostMealSatietyLevel,
            [],
            []);

    private static ExportDiaryQueryHandler CreateHandler(IReadOnlyList<Meal> meals) =>
        CreateHandler(CreateExportDiaryReadService(meals));

    private static ExportDiaryQueryHandler CreateHandler(IExportDiaryReadService diaryReadService) =>
        new(diaryReadService, CreateCurrentUserAccessService(), CreatePdfGenerator(out _));

    [Fact]
    public async Task ExportDiary_WithMeals_ReturnsCsvFileResult() {
        var userId = UserId.New();
        Meal[] meals = [CreateMeal(userId), CreateMeal(userId, TestDate.AddDays(1), MealType.Lunch)];
        ExportDiaryQueryHandler handler = CreateHandler(meals);

        Result<FileExportResult> result = await handler.Handle(
            new ExportDiaryQuery(userId.Value, TestDate, TestDate.AddDays(1)),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal("text/csv", result.Value.ContentType);
        Assert.Contains("food-diary-", result.Value.FileName, StringComparison.Ordinal);
        Assert.EndsWith(".csv", result.Value.FileName, StringComparison.Ordinal);
        Assert.True(result.Value.Content.Length > 0);
    }

    [Fact]
    public async Task ExportDiary_WithPdfFormat_ReturnsPdfFileResult() {
        var userId = UserId.New();
        Meal[] meals = [CreateMeal(userId)];
        IDiaryPdfGenerator pdfGenerator = CreatePdfGenerator(out Func<(string? Locale, int? TimeZoneOffsetMinutes, string? ReportOrigin)> getPdfCall);
        var handler = new ExportDiaryQueryHandler(CreateExportDiaryReadService(meals), CreateCurrentUserAccessService(), pdfGenerator);

        Result<FileExportResult> result = await handler.Handle(
            new ExportDiaryQuery(userId.Value, TestDate, TestDate.AddDays(1), ExportFormat.Pdf, "ru", 240, "https://Ð´Ð½ÐµÐ²Ð½Ð¸ÐºÐµÐ´Ñ‹.Ñ€Ñ„"),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal("application/pdf", result.Value.ContentType);
        Assert.EndsWith(".pdf", result.Value.FileName, StringComparison.Ordinal);
        (string? locale, int? timeZoneOffsetMinutes, string? reportOrigin) = getPdfCall();
        Assert.Equal("ru", locale);
        Assert.Equal(240, timeZoneOffsetMinutes);
        Assert.Equal("https://Ð´Ð½ÐµÐ²Ð½Ð¸ÐºÐµÐ´Ñ‹.Ñ€Ñ„", reportOrigin);
    }

    [Fact]
    public async Task ExportDiary_WithUnsafeReportOrigin_DropsOriginBeforePdfGeneration() {
        var userId = UserId.New();
        IDiaryPdfGenerator pdfGenerator = CreatePdfGenerator(out Func<(string? Locale, int? TimeZoneOffsetMinutes, string? ReportOrigin)> getPdfCall);
        var handler = new ExportDiaryQueryHandler(CreateExportDiaryReadService([]), CreateCurrentUserAccessService(), pdfGenerator);

        Result<FileExportResult> result = await handler.Handle(
            new ExportDiaryQuery(userId.Value, TestDate, TestDate.AddDays(1), ExportFormat.Pdf, ReportOrigin: "javascript:alert(1)"),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Null(getPdfCall().ReportOrigin);
    }

    [Fact]
    public async Task ExportDiary_WithLocalDayUtcBoundaries_PreservesRequestedInstants() {
        var userId = UserId.New();
        IMealRepository repository = CreateMealRepository([], out Func<(DateTime? DateFrom, DateTime? DateTo)> getLastPeriod);
        ExportDiaryQueryHandler handler = CreateHandler(new ExportDiaryReadService(repository));
        DateTime localDayStartUtc = new DateTimeOffset(2026, 5, 4, 0, 0, 0, TimeSpan.FromHours(4)).UtcDateTime;
        DateTime localDayEndUtc = new DateTimeOffset(2026, 5, 4, 23, 59, 59, 999, TimeSpan.FromHours(4)).UtcDateTime;

        Result<FileExportResult> result = await handler.Handle(
            new ExportDiaryQuery(userId.Value, localDayStartUtc, localDayEndUtc, ExportFormat.Pdf),
            CancellationToken.None);

        ResultAssert.Success(result);
        (DateTime? dateFrom, DateTime? dateTo) = getLastPeriod();
        Assert.Equal(localDayStartUtc, dateFrom);
        Assert.Equal(localDayEndUtc, dateTo);
    }

    [Fact]
    public async Task ExportDiary_WithRepositoryOverfetch_FiltersToRequestedInstants() {
        var userId = UserId.New();
        DateTime localDayStartUtc = new DateTimeOffset(2026, 5, 4, 0, 0, 0, TimeSpan.FromHours(4)).UtcDateTime;
        DateTime localDayEndUtc = new DateTimeOffset(2026, 5, 4, 23, 59, 59, 999, TimeSpan.FromHours(4)).UtcDateTime;
        Meal beforeMeal = CreateMeal(userId, localDayStartUtc.AddMinutes(-1), comment: "outside before");
        Meal includedMeal = CreateMeal(userId, localDayStartUtc.AddMinutes(1), comment: "inside period");
        Meal afterMeal = CreateMeal(userId, localDayEndUtc.AddMilliseconds(1), comment: "outside after");
        ExportDiaryQueryHandler handler = CreateHandler([beforeMeal, includedMeal, afterMeal]);

        Result<FileExportResult> result = await handler.Handle(
            new ExportDiaryQuery(userId.Value, localDayStartUtc, localDayEndUtc),
            CancellationToken.None);

        ResultAssert.Success(result);
        string content = System.Text.Encoding.UTF8.GetString(result.Value.Content);
        Assert.Contains("inside period", content, StringComparison.Ordinal);
        Assert.DoesNotContain("outside before", content, StringComparison.Ordinal);
        Assert.DoesNotContain("outside after", content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExportDiary_WithCsvFormat_UsesLocalDatesForFilenameAndRows() {
        var userId = UserId.New();
        DateTime localDayStartUtc = new DateTimeOffset(2026, 5, 4, 0, 0, 0, TimeSpan.FromHours(4)).UtcDateTime;
        DateTime localDayEndUtc = new DateTimeOffset(2026, 5, 4, 23, 59, 59, 999, TimeSpan.FromHours(4)).UtcDateTime;
        Meal meal = CreateMeal(userId, localDayStartUtc.AddHours(1), comment: "local day meal");
        ExportDiaryQueryHandler handler = CreateHandler([meal]);

        Result<FileExportResult> result = await handler.Handle(
            new ExportDiaryQuery(userId.Value, localDayStartUtc, localDayEndUtc, ExportFormat.Csv, TimeZoneOffsetMinutes: 240),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Contains("food-diary-2026-05-04-to-2026-05-04.csv", result.Value.FileName, StringComparison.Ordinal);
        string content = System.Text.Encoding.UTF8.GetString(result.Value.Content);
        Assert.Contains("2026-05-04,Breakfast", content, StringComparison.Ordinal);
        Assert.Contains("local day meal", content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExportDiary_WithMissingTimeZoneOffset_InfersDisplayDateFromRangeStart() {
        var userId = UserId.New();
        DateTime localDayStartUtc = new DateTimeOffset(2026, 5, 4, 0, 0, 0, TimeSpan.FromHours(4)).UtcDateTime;
        DateTime localDayEndUtc = new DateTimeOffset(2026, 5, 4, 23, 59, 59, 999, TimeSpan.FromHours(4)).UtcDateTime;
        Meal meal = CreateMeal(userId, localDayStartUtc.AddHours(1));
        ExportDiaryQueryHandler handler = CreateHandler([meal]);

        Result<FileExportResult> result = await handler.Handle(
            new ExportDiaryQuery(userId.Value, localDayStartUtc, localDayEndUtc),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Contains("food-diary-2026-05-04-to-2026-05-04.csv", result.Value.FileName, StringComparison.Ordinal);
        string content = System.Text.Encoding.UTF8.GetString(result.Value.Content);
        Assert.Contains("2026-05-04,Breakfast", content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExportDiary_WithNoMeals_ReturnsHeaderOnly() {
        var userId = UserId.New();
        ExportDiaryQueryHandler handler = CreateHandler([]);

        Result<FileExportResult> result = await handler.Handle(
            new ExportDiaryQuery(userId.Value, TestDate, TestDate.AddDays(1)),
            CancellationToken.None);

        ResultAssert.Success(result);
        string content = System.Text.Encoding.UTF8.GetString(result.Value.Content);
        Assert.Contains("Date,MealType,Calories", content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExportDiary_WithNullUserId_ReturnsFailure() {
        ExportDiaryQueryHandler handler = CreateHandler([]);

        Result<FileExportResult> result = await handler.Handle(
            new ExportDiaryQuery(UserId: null, TestDate, TestDate.AddDays(1)),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task ExportDiary_WithDateFromAfterDateTo_ReturnsValidationFailure() {
        var userId = UserId.New();
        ExportDiaryQueryHandler handler = CreateHandler([]);

        Result<FileExportResult> result = await handler.Handle(
            new ExportDiaryQuery(userId.Value, TestDate.AddDays(1), TestDate),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public async Task ExportDiary_WithRangeOverOneYear_ReturnsValidationFailure() {
        var userId = UserId.New();
        ExportDiaryQueryHandler handler = CreateHandler([]);

        Result<FileExportResult> result = await handler.Handle(
            new ExportDiaryQuery(userId.Value, TestDate, TestDate.AddDays(367)),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public async Task ExportDiary_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("export-deleted@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new ExportDiaryQueryHandler(
            CreateExportDiaryReadService([]),
            CreateCurrentUserAccessService(user),
            CreatePdfGenerator(out _));

        Result<FileExportResult> result = await handler.Handle(
            new ExportDiaryQuery(user.Id.Value, TestDate, TestDate.AddDays(1)),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task ExportCycle_WithCurrentProfile_ReturnsCycleCsv() {
        var userId = UserId.New();
        var profile = CycleProfile.Create(userId, TestDate);
        profile.UpsertBleedingEntry(TestDate, BleedingType.Bleeding, CycleFlowLevel.Heavy, painImpact: 8, notes: "heavy, painful");
        profile.UpsertSymptomEntry(TestDate, CycleSymptomCategory.Mood, 6, ["irritable"], "low mood");
        profile.UpsertFertilitySignal(TestDate, 36.62, OvulationTestResult.Positive, "egg white", hadSex: true, notes: "signal note");
        profile.UpsertFactor(CycleFactorType.HormonalContraception, TestDate.AddDays(-1), endDate: null, notes: "pill");
        ExportCycleQueryHandler handler = CreateExportCycleHandler(profile, CreateCurrentUserAccessService());

        Result<FileExportResult> result = await handler.Handle(
            new ExportCycleQuery(userId.Value, TestDate, TestDate.AddDays(1)),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal("text/csv", result.Value.ContentType);
        Assert.EndsWith(".csv", result.Value.FileName, StringComparison.Ordinal);
        string content = System.Text.Encoding.UTF8.GetString(result.Value.Content);
        Assert.Contains("RecordType,Date,EndDate,Category", content, StringComparison.Ordinal);
        Assert.Contains("Bleeding,2026-04-01,,Bleeding,,Heavy,8", content, StringComparison.Ordinal);
        Assert.Contains("\"heavy, painful\"", content, StringComparison.Ordinal);
        Assert.Contains("Symptom,2026-04-01,,Mood,irritable,,6", content, StringComparison.Ordinal);
        Assert.Contains("FertilitySignal,2026-04-01,,,,,,36.62,Positive,egg white,True,signal note", content, StringComparison.Ordinal);
        Assert.Contains("Factor,2026-03-31,,HormonalContraception", content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExportCycle_WithValidTimeZoneOffset_UsesOffsetForFileName() {
        var userId = UserId.New();
        var profile = CycleProfile.Create(userId, TestDate);
        ExportCycleQueryHandler handler = CreateExportCycleHandler(profile, CreateCurrentUserAccessService());

        Result<FileExportResult> result = await handler.Handle(
            new ExportCycleQuery(
                userId.Value,
                new DateTime(2026, 5, 3, 21, 0, 0, DateTimeKind.Utc),
                new DateTime(2026, 5, 4, 20, 0, 0, DateTimeKind.Utc),
                TimeZoneOffsetMinutes: 240),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Contains("cycle-tracking-2026-05-04-to-2026-05-05.csv", result.Value.FileName, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExportCycle_WithNoCurrentProfile_ReturnsNotFound() {
        var userId = UserId.New();
        ExportCycleQueryHandler handler = CreateExportCycleHandler(profile: null, CreateCurrentUserAccessService());

        Result<FileExportResult> result = await handler.Handle(
            new ExportCycleQuery(userId.Value, TestDate, TestDate.AddDays(1)),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Cycle.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task ExportCycle_WithNullUserId_ReturnsFailure() {
        ExportCycleQueryHandler handler = CreateExportCycleHandler(profile: null, CreateCurrentUserAccessService());

        Result<FileExportResult> result = await handler.Handle(
            new ExportCycleQuery(UserId: null, TestDate, TestDate.AddDays(1)),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task ExportCycle_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("cycle-export-deleted@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        ExportCycleQueryHandler handler = CreateExportCycleHandler(
            profile: null,
            CreateCurrentUserAccessService(user));

        Result<FileExportResult> result = await handler.Handle(
            new ExportCycleQuery(user.Id.Value, TestDate, TestDate.AddDays(1)),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task ExportCycle_WithDateFromAfterDateTo_ReturnsValidationFailure() {
        var userId = UserId.New();
        ExportCycleQueryHandler handler = CreateExportCycleHandler(profile: null, CreateCurrentUserAccessService());

        Result<FileExportResult> result = await handler.Handle(
            new ExportCycleQuery(userId.Value, TestDate.AddDays(1), TestDate),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public async Task ExportCycle_WithRangeOverOneYear_ReturnsValidationFailure() {
        var userId = UserId.New();
        ExportCycleQueryHandler handler = CreateExportCycleHandler(profile: null, CreateCurrentUserAccessService());

        Result<FileExportResult> result = await handler.Handle(
            new ExportCycleQuery(userId.Value, TestDate, TestDate.AddDays(367)),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public void CsvGenerator_WithAutoCalculated_UsesTotals() {
        Meal meal = CreateMeal();

        byte[] csv = DiaryCsvGenerator.Generate([ToReadModel(meal)]);
        string content = System.Text.Encoding.UTF8.GetString(csv);

        Assert.Contains("500", content, StringComparison.Ordinal);
        Assert.Contains("30", content, StringComparison.Ordinal);
        Assert.Contains("Breakfast", content, StringComparison.Ordinal);
    }

    [Fact]
    public void CsvGenerator_WithManualOverride_UsesManualValues() {
        Meal meal = CreateMeal();
        meal.ApplyNutrition(new MealNutritionUpdate(
            500, 30, 20, 60, 5, 0,
            IsAutoCalculated: false,
            ManualCalories: 400, ManualProteins: 25, ManualFats: 15,
            ManualCarbs: 50, ManualFiber: 3, ManualAlcohol: 0));

        byte[] csv = DiaryCsvGenerator.Generate([ToReadModel(meal)]);
        string content = System.Text.Encoding.UTF8.GetString(csv);

        Assert.Contains("400", content, StringComparison.Ordinal);
        Assert.Contains("25", content, StringComparison.Ordinal);
    }

    [Fact]
    public void CsvGenerator_WithCommaInComment_EscapesProperly() {
        Meal meal = CreateMeal(comment: "eggs, bacon");

        byte[] csv = DiaryCsvGenerator.Generate([ToReadModel(meal)]);
        string content = System.Text.Encoding.UTF8.GetString(csv);

        Assert.Contains("\"eggs, bacon\"", content, StringComparison.Ordinal);
    }

    [Fact]
    public void CsvGenerator_WithQuoteInComment_EscapesProperly() {
        Meal meal = CreateMeal(comment: "she said \"hello\"");

        byte[] csv = DiaryCsvGenerator.Generate([ToReadModel(meal)]);
        string content = System.Text.Encoding.UTF8.GetString(csv);

        Assert.Contains("\"she said \"\"hello\"\"\"", content, StringComparison.Ordinal);
    }

    [Fact]
    public void CsvGenerator_WithNewlineInComment_EscapesProperly() {
        Meal meal = CreateMeal(comment: "first line\nsecond line");

        byte[] csv = DiaryCsvGenerator.Generate([ToReadModel(meal)]);
        string content = System.Text.Encoding.UTF8.GetString(csv);

        Assert.Contains("\"first line\nsecond line\"", content, StringComparison.Ordinal);
    }

    [Fact]
    public void CsvGenerator_WithNoMealType_WritesEmptyField() {
        Meal meal = CreateMeal(mealType: null);

        byte[] csv = DiaryCsvGenerator.Generate([ToReadModel(meal)]);
        string[] lines = System.Text.Encoding.UTF8.GetString(csv).Split('\n', StringSplitOptions.RemoveEmptyEntries);

        Assert.True(lines.Length >= 2);
        string dataLine = lines[1];
        Assert.Contains(",,", dataLine, StringComparison.Ordinal);
    }

    [Fact]
    public void CsvGenerator_WithTimeZoneOffset_WritesDisplayDate() {
        Meal meal = CreateMeal(date: new DateTime(2026, 5, 3, 21, 0, 0, DateTimeKind.Utc));

        byte[] csv = DiaryCsvGenerator.Generate([ToReadModel(meal)], timeZoneOffsetMinutes: 240);
        string content = System.Text.Encoding.UTF8.GetString(csv);

        Assert.Contains("2026-05-04,Breakfast", content, StringComparison.Ordinal);
    }

    [Fact]
    public void CsvGenerator_WithInvalidTimeZoneOffset_FallsBackToUtc() {
        Meal meal = CreateMeal(date: new DateTime(2026, 5, 3, 21, 0, 0, DateTimeKind.Utc));

        byte[] csv = DiaryCsvGenerator.Generate([ToReadModel(meal)], timeZoneOffsetMinutes: 900);
        string content = System.Text.Encoding.UTF8.GetString(csv);

        Assert.Contains("2026-05-03,Breakfast", content, StringComparison.Ordinal);
    }

    [Fact]
    public void CsvGenerator_WithUnspecifiedDate_TreatsValueAsUtc() {
        Meal meal = CreateMeal();
        SetMealDate(meal, new DateTime(2026, 5, 3, 21, 0, 0, DateTimeKind.Unspecified));

        byte[] csv = DiaryCsvGenerator.Generate([ToReadModel(meal)], TimeSpan.FromHours(4));
        string content = System.Text.Encoding.UTF8.GetString(csv);

        Assert.Contains("2026-05-04,Breakfast", content, StringComparison.Ordinal);
    }

    [Fact]
    public void CsvGenerator_WithLocalDate_ConvertsValueToUtcBeforeApplyingOffset() {
        var localDate = DateTime.SpecifyKind(new DateTime(2026, 5, 3, 21, 0, 0), DateTimeKind.Local);
        Meal meal = CreateMeal();
        SetMealDate(meal, localDate);

        byte[] csv = DiaryCsvGenerator.Generate([ToReadModel(meal)], TimeSpan.Zero);
        string content = System.Text.Encoding.UTF8.GetString(csv);
        string expectedDate = localDate.ToUniversalTime().ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

        Assert.Contains($"{expectedDate},Breakfast", content, StringComparison.Ordinal);
    }

    [Fact]
    public void CsvGenerator_HasUtf8Bom() {
        byte[] csv = DiaryCsvGenerator.Generate([]);

        Assert.True(csv.Length >= 3);
        Assert.Equal(0xEF, csv[0]);
        Assert.Equal(0xBB, csv[1]);
        Assert.Equal(0xBF, csv[2]);
    }

    private static IMealRepository CreateMealRepository(IReadOnlyList<Meal> meals) =>
        CreateMealRepository(meals, out _);

    private static IExportDiaryReadService CreateExportDiaryReadService(IReadOnlyList<Meal> meals) =>
        new ExportDiaryReadService(CreateMealRepository(meals));

    private static IMealRepository CreateMealRepository(
        IReadOnlyList<Meal> meals,
        out Func<(DateTime? DateFrom, DateTime? DateTo)> getLastPeriod) {
        DateTime? lastDateFrom = null;
        DateTime? lastDateTo = null;
        getLastPeriod = () => (lastDateFrom, lastDateTo);

        IMealRepository repository = Substitute.For<IMealRepository>();
        repository
            .GetByPeriodConsumptionReadModelsAsync(Arg.Any<UserId>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                lastDateFrom = call.ArgAt<DateTime>(1);
                lastDateTo = call.ArgAt<DateTime>(2);
                return Task.FromResult<IReadOnlyList<MealConsumptionReadModel>>([.. meals.Select(ToReadModel)]);
            });

        return repository;
    }

    private static ICurrentUserAccessService CreateCurrentUserAccessService(User? user = null) {
        ICurrentUserAccessService service = Substitute.For<ICurrentUserAccessService>();
        service
            .EnsureCanAccessAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                UserId userId = call.Arg<UserId>();
                Error? error = user switch {
                    null => null,
                    { Id: var id } when id != userId => Errors.Authentication.InvalidToken,
                    { DeletedAt: not null } => Errors.Authentication.AccountDeleted,
                    _ => null,
                };
                return Task.FromResult(error);
            });

        return service;
    }

    private static ICycleReadModelRepository CreateCycleRepository(CycleProfile? profile) {
        ICycleRepository repository = Substitute.For<ICycleRepository>();
        repository
            .GetCurrentReadModelAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                UserId userId = call.ArgAt<UserId>(0);
                return Task.FromResult(profile is null || profile.UserId != userId ? null : ToReadModel(profile));
            });

        return repository;
    }

    private static CycleProfileReadModel ToReadModel(CycleProfile profile) =>
        new(
            profile.Id.Value,
            profile.UserId.Value,
            profile.Mode,
            profile.Confidence,
            profile.TrackingStartDate,
            profile.AverageCycleLength,
            profile.AveragePeriodLength,
            profile.LutealLength,
            profile.IsRegular,
            profile.IsOnboardingComplete,
            profile.ShowFertilityEstimates,
            profile.DiscreetNotifications,
            profile.Notes,
            [.. profile.BleedingEntries.Select(static entry => new BleedingEntryReadModel(
                entry.Id.Value,
                entry.CycleProfileId.Value,
                entry.Date,
                entry.Type,
                entry.Flow,
                entry.PainImpact,
                entry.Notes))],
            [.. profile.SymptomEntries.Select(static entry => new CycleSymptomEntryReadModel(
                entry.Id.Value,
                entry.CycleProfileId.Value,
                entry.Date,
                entry.Category,
                entry.Intensity,
                entry.Tags,
                entry.Note))],
            [.. profile.Factors.Select(static factor => new CycleFactorReadModel(
                factor.Id.Value,
                factor.CycleProfileId.Value,
                factor.Type,
                factor.StartDate,
                factor.EndDate,
                factor.Notes))],
            [.. profile.FertilitySignals.Select(static signal => new FertilitySignalReadModel(
                signal.Id.Value,
                signal.CycleProfileId.Value,
                signal.Date,
                signal.BasalBodyTemperatureCelsius,
                signal.OvulationTestResult,
                signal.CervicalFluid,
                signal.HadSex,
                signal.Notes))]);

    private static ExportCycleQueryHandler CreateExportCycleHandler(
        CycleProfile? profile,
        ICurrentUserAccessService currentUserAccessService) =>
        new(
            new CycleReadService(CreateCycleRepository(profile), Substitute.For<IDashboardStatisticsReadService>()),
            currentUserAccessService);

    private static IDiaryPdfGenerator CreatePdfGenerator(
        out Func<(string? Locale, int? TimeZoneOffsetMinutes, string? ReportOrigin)> getLastCall) {
        string? lastLocale = null;
        int? lastTimeZoneOffsetMinutes = null;
        string? lastReportOrigin = null;
        getLastCall = () => (lastLocale, lastTimeZoneOffsetMinutes, lastReportOrigin);

        IDiaryPdfGenerator generator = Substitute.For<IDiaryPdfGenerator>();
        generator
            .GenerateAsync(
                Arg.Any<IReadOnlyList<MealConsumptionReadModel>>(),
                Arg.Any<DateTime>(),
                Arg.Any<DateTime>(),
                Arg.Any<string?>(),
                Arg.Any<int?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(call => {
                lastLocale = call.ArgAt<string?>(3);
                lastTimeZoneOffsetMinutes = call.ArgAt<int?>(4);
                lastReportOrigin = call.ArgAt<string?>(5);
                return Task.FromResult<byte[]>([0x25, 0x50, 0x44, 0x46]); // %PDF magic bytes
            });

        return generator;
    }

    private static void SetMealDate(Meal meal, DateTime date) {
        typeof(Meal)
            .GetProperty(nameof(Meal.Date))!
            .SetValue(meal, date);
    }
}
