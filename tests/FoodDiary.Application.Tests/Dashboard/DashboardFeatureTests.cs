using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FluentValidation.Results;
using System.Globalization;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Dashboard.Models;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Hydration.Common;
using FoodDiary.Application.Abstractions.WaistEntries.Common;
using FoodDiary.Application.Abstractions.WeightEntries.Models;
using FoodDiary.Application.Abstractions.WeightEntries.Common;
using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Consumptions.Models;
using FoodDiary.Application.Consumptions.Queries.GetConsumptions;
using FoodDiary.Application.Dashboard.Commands.SendDashboardTestEmail;
using FoodDiary.Application.Dashboard.Common;
using FoodDiary.Application.Dashboard.Models;
using FoodDiary.Application.Dashboard.Queries.GetDashboardSnapshot;
using FoodDiary.Application.Dashboard.Services;
using FoodDiary.Application.Statistics.Models;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Mediator;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.Application.Tests.Dashboard;

[ExcludeFromCodeCoverage]
public class DashboardFeatureTests {
    [Fact]
    public async Task GetDashboardSnapshotQueryValidator_WithEmptyUserId_Fails() {
        var validator = new GetDashboardSnapshotQueryValidator();
        var query = new GetDashboardSnapshotQuery(
            UserId.Empty,
            DateTime.UtcNow,
            Page: 1,
            PageSize: 10,
            Locale: "en",
            TrendDays: 7);

        ValidationResult result = await validator.ValidateAsync(query);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task GetDashboardSnapshotQueryValidator_WithValidInput_Passes() {
        var validator = new GetDashboardSnapshotQueryValidator();
        var query = new GetDashboardSnapshotQuery(
            UserId.New(),
            DateTime.UtcNow,
            Page: 1,
            PageSize: 10,
            Locale: "en",
            TrendDays: 7);

        ValidationResult result = await validator.ValidateAsync(query);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task SendDashboardTestEmailCommandValidator_WithEmptyUserId_Fails() {
        var validator = new SendDashboardTestEmailCommandValidator();

        ValidationResult result = await validator.ValidateAsync(new SendDashboardTestEmailCommand(Guid.Empty));

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            error => string.Equals(error.ErrorCode, "Authentication.InvalidToken", StringComparison.Ordinal));
    }

    [Fact]
    public void DashboardMapping_ToStatisticsModel_WhenResponseIsNull_ReturnsEmptyModel() {
        DashboardStatisticsModel dto = DashboardMapping.ToStatisticsModel((AggregatedStatisticsModel?)null, user: null);

        Assert.Equal(0, dto.TotalCalories);
        Assert.Equal(0, dto.AverageProteins);
        Assert.Null(dto.ProteinGoal);
        Assert.Null(dto.FiberGoal);
    }

    [Fact]
    public void DashboardMapping_ToStatisticsModel_WhenReadModelResponseIsNull_ReturnsEmptyModel() {
        DashboardStatisticsModel dto = DashboardMapping.ToStatisticsModel((DashboardStatisticsBucketReadModel?)null, user: null);

        Assert.Multiple(
            () => Assert.Equal(0, dto.TotalCalories),
            () => Assert.Equal(0, dto.AverageProteins),
            () => Assert.Null(dto.ProteinGoal),
            () => Assert.Null(dto.FiberGoal));
    }

    [Fact]
    public void DashboardMapping_ToStatisticsModel_MapsMacroTargetsFromUser() {
        var user = User.Create("dashboard-stats@example.com", "hash");
        user.UpdateGoals(proteinTarget: 120, fatTarget: 70, carbTarget: 210, fiberTarget: 30);
        var response = new AggregatedStatisticsModel(
            DateTime.UtcNow.Date,
            DateTime.UtcNow.Date,
            1900,
            110,
            65,
            200,
            28);

        DashboardStatisticsModel dto = DashboardMapping.ToStatisticsModel(response, CreateDashboardUserContext(user));

        Assert.Equal(1900, dto.TotalCalories);
        Assert.Equal(110, dto.AverageProteins);
        Assert.Equal(120, dto.ProteinGoal);
        Assert.Equal(70, dto.FatGoal);
        Assert.Equal(210, dto.CarbGoal);
        Assert.Equal(30, dto.FiberGoal);
    }

    [Fact]
    public void DashboardMapping_ToWeeklyCalories_OrdersByDateAscending() {
        var day1 = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime day2 = day1.AddDays(1);
        var responses = new List<AggregatedStatisticsModel> {
            new(day2, day2, 2000, 100, 70, 250, 30),
            new(day1, day1, 1800, 90, 60, 220, 25),
        };

        IReadOnlyList<DailyCaloriesModel> calories = DashboardMapping.ToWeeklyCalories(responses);

        Assert.Collection(
            calories,
            c => Assert.Equal(day1, c.Date),
            c => Assert.Equal(day2, c.Date));
    }

    [Fact]
    public void DashboardMapping_ToWeightModel_MapsLatestAndPreviousEntries() {
        var userId = UserId.New();
        var latestDate = new DateTime(2026, 2, 20, 0, 0, 0, DateTimeKind.Utc);
        DateTime previousDate = latestDate.AddDays(-1);
        var entries = new List<DashboardWeightPointReadModel> {
            new(latestDate, 82.5),
            new(previousDate, 83),
        };

        DashboardWeightModel dto = DashboardMapping.ToWeightModel(entries, desired: 80);

        Assert.NotNull(dto.Latest);
        Assert.NotNull(dto.Previous);
        Assert.Equal(82.5, dto.Latest!.Weight);
        Assert.Equal(83, dto.Previous!.Weight);
        Assert.Equal(80, dto.Desired);
    }

    [Fact]
    public void DashboardMapping_ToWeightModel_WithNoEntries_ReturnsEmptyPoints() {
        DashboardWeightModel dto = DashboardMapping.ToWeightModel(Array.Empty<DashboardWeightPointReadModel>(), desired: null);

        Assert.Null(dto.Latest);
        Assert.Null(dto.Previous);
        Assert.Null(dto.Desired);
    }

    [Fact]
    public async Task RepositoryDashboardBodyReadService_WithPartialFinalBucket_ClampsBucketEndToRangeEnd() {
        var userId = UserId.New();
        DateTime dayStart = new(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc);
        DateTime trendStart = dayStart.AddDays(-4);
        IWeightEntryRepository weightRepository = Substitute.For<IWeightEntryRepository>();
        weightRepository.GetEntryReadModelsAsync(
                userId,
                Arg.Any<DateTime?>(),
                dayStart,
                limit: 2,
                descending: true,
                cancellationToken: Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<WeightEntryReadModel>>([]));
        weightRepository.GetByPeriodReadModelsAsync(userId, trendStart, dayStart, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<WeightEntryReadModel>>([
                new(Guid.NewGuid(), userId.Value, trendStart, 80),
                new(Guid.NewGuid(), userId.Value, dayStart, 82),
            ]));
        RepositoryDashboardBodyReadService service = new(
            weightRepository,
            Substitute.For<IWaistEntryRepository>(),
            Substitute.For<IHydrationEntryRepository>());

        DashboardBodyReadModel result = await service.GetBodyAsync(
            userId,
            dayStart,
            dayStart,
            trendStart,
            trendQuantizationDays: 3,
            includeWeight: true,
            includeWaist: false,
            includeHydration: false,
            CancellationToken.None);

        Assert.Equal(2, result.WeightTrend.Count);
        Assert.Equal(dayStart, result.WeightTrend[1].DateTo);
    }

    [Fact]
    public void DashboardMapping_ToWaistModel_MapsLatestAndPreviousEntries() {
        var userId = UserId.New();
        var latestDate = new DateTime(2026, 2, 20, 0, 0, 0, DateTimeKind.Utc);
        DateTime previousDate = latestDate.AddDays(-1);
        var entries = new List<DashboardWaistPointReadModel> {
            new(latestDate, 92.1),
            new(previousDate, 92.8),
        };

        DashboardWaistModel dto = DashboardMapping.ToWaistModel(entries, desired: 90);

        Assert.NotNull(dto.Latest);
        Assert.NotNull(dto.Previous);
        Assert.Equal(92.1, dto.Latest!.Circumference);
        Assert.Equal(92.8, dto.Previous!.Circumference);
        Assert.Equal(90, dto.Desired);
    }

    [Fact]
    public void DashboardMapping_ToWaistModel_WithNoEntries_ReturnsEmptyPoints() {
        DashboardWaistModel dto = DashboardMapping.ToWaistModel(Array.Empty<DashboardWaistPointReadModel>(), desired: null);

        Assert.Null(dto.Latest);
        Assert.Null(dto.Previous);
        Assert.Null(dto.Desired);
    }

    [Fact]
    public void DashboardMapping_ToMealsModel_MapsNestedMealsAndOrdersChildren() {
        var mealId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var itemA = Guid.Parse("10000000-0000-0000-0000-000000000002");
        var itemB = Guid.Parse("10000000-0000-0000-0000-000000000003");
        var sessionA = Guid.Parse("10000000-0000-0000-0000-000000000004");
        var sessionB = Guid.Parse("10000000-0000-0000-0000-000000000005");
        var aiItemA = Guid.Parse("10000000-0000-0000-0000-000000000006");
        var aiItemB = Guid.Parse("10000000-0000-0000-0000-000000000007");
        DashboardMealReadModel meal = CreateDashboardMealReadModel(mealId, [
            CreateDashboardMealItemReadModel(itemB, mealId, amount: 2),
            CreateDashboardMealItemReadModel(itemA, mealId, amount: 1),
        ], [
            CreateDashboardMealAiSessionReadModel(sessionB, mealId, new DateTime(2026, 3, 2, 10, 0, 0, DateTimeKind.Utc), [
                CreateDashboardMealAiItemReadModel(aiItemB, sessionB, "later"),
            ]),
            CreateDashboardMealAiSessionReadModel(sessionA, mealId, new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc), [
                CreateDashboardMealAiItemReadModel(aiItemB, sessionA, "second"),
                CreateDashboardMealAiItemReadModel(aiItemA, sessionA, "first"),
            ]),
        ]);

        DashboardMealsModel model = DashboardMapping.ToMealsModel(new DashboardMealsReadModel([meal], Page: 1, Limit: 10, TotalPages: 1, TotalItems: 1));

        ConsumptionModel mappedMeal = Assert.Single(model.Items);
        Assert.Equal(1, model.Total);
        Assert.Equal(mealId, mappedMeal.Id);
        Assert.Equal(["manual", "ai"], mappedMeal.Items.Select(item => item.Origin));
        Assert.Equal([itemA, itemB], mappedMeal.Items.Select(item => item.Id));
        Assert.Equal([sessionA, sessionB], mappedMeal.AiSessions.Select(session => session.Id));
        Assert.Equal([aiItemA, aiItemB], mappedMeal.AiSessions[0].Items.Select(item => item.Id));
        Assert.Equal("first", mappedMeal.AiSessions[0].Items[0].NameEn);
    }

    [Fact]
    public async Task MediatorDashboardMealsReadService_WhenQuerySucceeds_MapsPagedMeals() {
        var userId = Guid.NewGuid();
        var mealId = Guid.NewGuid();
        ConsumptionModel meal = CreateConsumptionModel(mealId, [
            CreateConsumptionItemModel(Guid.NewGuid(), mealId, amount: 150, origin: "Manual"),
        ], [
            CreateConsumptionAiSessionModel(Guid.NewGuid(), mealId, [
                CreateConsumptionAiItemModel(Guid.NewGuid(), Guid.NewGuid(), "toast"),
            ]),
        ]);
        ISender sender = CreateConsumptionSender(
            Result.Success(new PagedResponse<ConsumptionModel>([meal], Page: 2, Limit: 5, TotalPages: 3, TotalItems: 11)),
            out Func<GetConsumptionsQuery?> getLastQuery,
            out Func<CancellationToken> getLastCancellationToken);
        var service = new MediatorDashboardMealsReadService(sender);
        using var cancellationTokenSource = new CancellationTokenSource();
        var dateFrom = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        var dateTo = new DateTime(2026, 4, 7, 0, 0, 0, DateTimeKind.Utc);

        Result<DashboardMealsReadModel> result = await service.GetMealsAsync(
            new UserId(userId),
            page: 2,
            limit: 5,
            dateFrom,
            dateTo,
            cancellationTokenSource.Token);

        DashboardMealsReadModel model = ResultAssert.Success(result);
        Assert.Equal(2, model.Page);
        Assert.Equal(5, model.Limit);
        Assert.Equal(3, model.TotalPages);
        Assert.Equal(11, model.TotalItems);
        DashboardMealReadModel mappedMeal = Assert.Single(model.Items);
        Assert.Equal(meal.Id, mappedMeal.Id);
        Assert.Equal(meal.Items[0].ProductName, mappedMeal.Items[0].ProductName);
        Assert.Equal(meal.AiSessions[0].Items[0].NameEn, mappedMeal.AiSessions[0].Items[0].NameEn);
        GetConsumptionsQuery query = Assert.IsType<GetConsumptionsQuery>(getLastQuery());
        Assert.Equal(userId, query.UserId);
        Assert.Equal(2, query.Page);
        Assert.Equal(5, query.Limit);
        Assert.Equal(dateFrom, query.DateFrom);
        Assert.Equal(dateTo, query.DateTo);
        Assert.Equal(cancellationTokenSource.Token, getLastCancellationToken());
    }

    [Fact]
    public async Task MediatorDashboardMealsReadService_WhenQueryFails_ReturnsFailure() {
        Error error = Errors.Validation.Invalid("dashboard", "Could not read meals.");
        ISender sender = CreateConsumptionSender(
            Result.Failure<PagedResponse<ConsumptionModel>>(error),
            out _,
            out _);
        var service = new MediatorDashboardMealsReadService(sender);

        Result<DashboardMealsReadModel> result = await service.GetMealsAsync(
            UserId.New(),
            page: 1,
            limit: 10,
            DateTime.UtcNow.Date,
            DateTime.UtcNow.Date,
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public async Task GetDashboardSnapshotQueryHandler_ForwardsRequestToBuilder() {
        var userId = UserId.New();
        var date = new DateTime(2026, 5, 6, 0, 0, 0, DateTimeKind.Utc);
        IDashboardSnapshotBuilder builder = CreateDashboardSnapshotBuilder(
            out Func<DashboardSnapshotRequest?> getLastRequest,
            out Func<CancellationToken> getLastCancellationToken);
        GetDashboardSnapshotQueryHandler handler = new(builder);
        using var cts = new CancellationTokenSource();

        Result<DashboardSnapshotModel> result = await handler.Handle(
            new GetDashboardSnapshotQuery(userId.Value, date, Page: 2, PageSize: 25, Locale: "ru", TrendDays: 14),
            cts.Token);

        ResultAssert.Success(result);
        DashboardSnapshotRequest request = Assert.IsType<DashboardSnapshotRequest>(getLastRequest());
        Assert.Equal(userId.Value, request.UserId);
        Assert.Equal(date, request.Date);
        Assert.Equal("ru", request.Locale);
        Assert.Equal(14, request.TrendDays);
        Assert.Equal(2, request.Page);
        Assert.Equal(25, request.PageSize);
        Assert.Equal(cts.Token, getLastCancellationToken());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task GetDashboardSnapshotQueryHandler_WithMissingUserId_ReturnsInvalidToken(string? userIdText) {
        IDashboardSnapshotBuilder builder = CreateDashboardSnapshotBuilder(out Func<DashboardSnapshotRequest?> getLastRequest, out _);
        GetDashboardSnapshotQueryHandler handler = new(builder);
        Guid? userId = userIdText is null ? (Guid?)null : Guid.Parse(userIdText);

        Result<DashboardSnapshotModel> result = await handler.Handle(
            new GetDashboardSnapshotQuery(userId, DateTime.UtcNow, Page: 1, PageSize: 10, Locale: "en", TrendDays: 7),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
        Assert.Null(getLastRequest());
    }

    [Fact]
    public async Task SendDashboardTestEmail_WhenEmailSenderFails_ReturnsValidationFailure() {
        var user = User.Create("dashboard-email@example.com", "hash");
        var handler = new SendDashboardTestEmailCommandHandler(
            CreateUserRepository(user),
            CreateThrowingEmailSender(),
            NullLogger<SendDashboardTestEmailCommandHandler>.Instance);

        Result result = await handler.Handle(new SendDashboardTestEmailCommand(user.Id.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("TestEmail", result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendDashboardTestEmail_WithAccessibleUser_SendsToUserEmailAndLanguage() {
        var user = User.Create("dashboard-email-ok@example.com", "hash");
        user.SetLanguage("ru");
        IEmailSender emailSender = CreateEmailSender(out Func<TestEmailMessage?> getLastMessage);
        var handler = new SendDashboardTestEmailCommandHandler(
            CreateUserRepository(user),
            emailSender,
            NullLogger<SendDashboardTestEmailCommandHandler>.Instance);

        Result result = await handler.Handle(new SendDashboardTestEmailCommand(user.Id.Value), CancellationToken.None);

        ResultAssert.Success(result);
        TestEmailMessage message = Assert.IsType<TestEmailMessage>(getLastMessage());
        Assert.Equal("dashboard-email-ok@example.com", message.ToEmail);
        Assert.Equal("ru", message.Language);
    }

    [Fact]
    public async Task SendDashboardTestEmail_WhenUserMissing_ReturnsInvalidToken() {
        IEmailSender emailSender = CreateEmailSender(out Func<TestEmailMessage?> getLastMessage);
        var handler = new SendDashboardTestEmailCommandHandler(
            CreateUserRepository(user: null),
            emailSender,
            NullLogger<SendDashboardTestEmailCommandHandler>.Instance);

        Result result = await handler.Handle(new SendDashboardTestEmailCommand(Guid.NewGuid()), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
        Assert.Null(getLastMessage());
    }

    private static IDashboardSnapshotBuilder CreateDashboardSnapshotBuilder(
        out Func<DashboardSnapshotRequest?> getLastRequest,
        out Func<CancellationToken> getLastCancellationToken) {
        IDashboardSnapshotBuilder builder = Substitute.For<IDashboardSnapshotBuilder>();
        DashboardSnapshotRequest? lastRequest = null;
        CancellationToken lastCancellationToken = default;
        builder
            .BuildAsync(
                Arg.Do<DashboardSnapshotRequest>(request => lastRequest = request),
                Arg.Do<CancellationToken>(cancellationToken => lastCancellationToken = cancellationToken))
            .Returns(Task.FromResult(Result.Success<DashboardSnapshotModel>(null!)));
        getLastRequest = () => lastRequest;
        getLastCancellationToken = () => lastCancellationToken;
        return builder;
    }

    private static IEmailSender CreateEmailSender(out Func<TestEmailMessage?> getLastMessage) {
        IEmailSender emailSender = Substitute.For<IEmailSender>();
        TestEmailMessage? lastMessage = null;
        emailSender
            .SendTestEmailAsync(Arg.Do<TestEmailMessage>(message => lastMessage = message), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        getLastMessage = () => lastMessage;
        return emailSender;
    }

    private static IEmailSender CreateThrowingEmailSender() {
        IEmailSender emailSender = Substitute.For<IEmailSender>();
        emailSender
            .SendTestEmailAsync(Arg.Any<TestEmailMessage>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("send failed")));
        return emailSender;
    }

    private static IDashboardUserContextService CreateUserRepository(User? user) {
        IDashboardUserContextService repository = Substitute.For<IDashboardUserContextService>();
        repository
            .GetAccessibleUserAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                UserId id = call.Arg<UserId>();
                return Task.FromResult(user is not null && user.Id == id
                    ? Result.Success(user)
                    : Result.Failure<User>(Errors.Authentication.InvalidToken));
            });
        return repository;
    }

    private static ISender CreateConsumptionSender(
        Result<PagedResponse<ConsumptionModel>> result,
        out Func<GetConsumptionsQuery?> getLastQuery,
        out Func<CancellationToken> getLastCancellationToken) {
        ISender sender = Substitute.For<ISender>();
        GetConsumptionsQuery? lastQuery = null;
        CancellationToken lastCancellationToken = default;
        sender
            .Send(
                Arg.Do<IRequest<Result<PagedResponse<ConsumptionModel>>>>(request => lastQuery = Assert.IsType<GetConsumptionsQuery>(request)),
                Arg.Do<CancellationToken>(cancellationToken => lastCancellationToken = cancellationToken))
            .Returns(Task.FromResult(result));
        getLastQuery = () => lastQuery;
        getLastCancellationToken = () => lastCancellationToken;
        return sender;
    }

    private static ConsumptionModel CreateConsumptionModel(
        Guid mealId,
        IReadOnlyList<ConsumptionItemModel> items,
        IReadOnlyList<ConsumptionAiSessionModel> sessions) =>
        new(
            mealId,
            new DateTime(2026, 3, 3, 12, 0, 0, DateTimeKind.Utc),
            "Breakfast",
            "Comment",
            "https://example.test/meal.webp",
            Guid.NewGuid(),
            500,
            30,
            20,
            40,
            8,
            0,
            IsNutritionAutoCalculated: false,
            ManualCalories: 510,
            ManualProteins: 31,
            ManualFats: 21,
            ManualCarbs: 41,
            ManualFiber: 9,
            ManualAlcohol: 0,
            PreMealSatietyLevel: 3,
            PostMealSatietyLevel: 8,
            QualityScore: 90,
            QualityGrade: "excellent",
            IsFavorite: true,
            FavoriteMealId: Guid.NewGuid(),
            items,
            sessions);

    private static ConsumptionItemModel CreateConsumptionItemModel(Guid itemId, Guid mealId, double amount, string origin) =>
        new(
            itemId,
            mealId,
            amount,
            ProductId: Guid.NewGuid(),
            ProductName: $"Product {amount.ToString(CultureInfo.InvariantCulture)}",
            ProductImageUrl: "https://example.test/product.webp",
            ProductBaseUnit: "g",
            ProductBaseAmount: 100,
            ProductCaloriesPerBase: 200,
            ProductProteinsPerBase: 10,
            ProductFatsPerBase: 5,
            ProductCarbsPerBase: 25,
            ProductFiberPerBase: 3,
            ProductAlcoholPerBase: 0,
            RecipeId: Guid.NewGuid(),
            RecipeName: "Recipe",
            RecipeImageUrl: "https://example.test/recipe.webp",
            RecipeServings: 2,
            RecipeTotalCalories: 400,
            RecipeTotalProteins: 20,
            RecipeTotalFats: 10,
            RecipeTotalCarbs: 50,
            RecipeTotalFiber: 6,
            RecipeTotalAlcohol: 0,
            ProductQualityScore: 80,
            ProductQualityGrade: "good",
            SourceAiItemId: Guid.NewGuid(),
            origin);

    private static ConsumptionAiSessionModel CreateConsumptionAiSessionModel(
        Guid sessionId,
        Guid mealId,
        IReadOnlyList<ConsumptionAiItemModel> items) =>
        new(
            sessionId,
            mealId,
            ImageAssetId: Guid.NewGuid(),
            ImageUrl: "https://example.test/ai.webp",
            Source: "Vision",
            Status: "Reviewed",
            RecognizedAtUtc: new DateTime(2026, 3, 3, 12, 5, 0, DateTimeKind.Utc),
            Notes: "looks right",
            items);

    private static ConsumptionAiItemModel CreateConsumptionAiItemModel(Guid itemId, Guid sessionId, string name) =>
        new(
            itemId,
            sessionId,
            name,
            NameLocal: "local",
            Amount: 1,
            Unit: "portion",
            Calories: 120,
            Proteins: 4,
            Fats: 3,
            Carbs: 20,
            Fiber: 2,
            Alcohol: 0,
            Confidence: 0.8,
            Resolution: "Accepted");

    private static DashboardUserContextModel CreateDashboardUserContext(User user) =>
        new(
            user.Id.Value,
            user.Email,
            user.Language,
            user.DashboardLayoutJson,
            user.DesiredWeight,
            user.DesiredWaist,
            user.HydrationGoal,
            user.WaterGoal,
            user.ProteinTarget,
            user.FatTarget,
            user.CarbTarget,
            user.FiberTarget,
            new UserCalorieSchedule(
                user.DailyCalorieTarget,
                user.CalorieCyclingEnabled,
                user.MondayCalories,
                user.TuesdayCalories,
                user.WednesdayCalories,
                user.ThursdayCalories,
                user.FridayCalories,
                user.SaturdayCalories,
                user.SundayCalories));

    private static DashboardMealReadModel CreateDashboardMealReadModel(
        Guid mealId,
        IReadOnlyList<DashboardMealItemReadModel> items,
        IReadOnlyList<DashboardMealAiSessionReadModel> sessions) =>
        new(
            mealId,
            new DateTime(2026, 3, 3, 12, 0, 0, DateTimeKind.Utc),
            "Lunch",
            "Read comment",
            "https://example.test/read-meal.webp",
            Guid.NewGuid(),
            600,
            35,
            25,
            55,
            10,
            0,
            IsNutritionAutoCalculated: true,
            ManualCalories: null,
            ManualProteins: null,
            ManualFats: null,
            ManualCarbs: null,
            ManualFiber: null,
            ManualAlcohol: null,
            PreMealSatietyLevel: 2,
            PostMealSatietyLevel: 7,
            IsFavorite: false,
            FavoriteMealId: null,
            items,
            sessions);

    private static DashboardMealItemReadModel CreateDashboardMealItemReadModel(Guid itemId, Guid mealId, double amount) =>
        new(
            itemId,
            mealId,
            amount,
            ProductId: Guid.NewGuid(),
            ProductName: $"Read product {amount.ToString(CultureInfo.InvariantCulture)}",
            ProductImageUrl: "https://example.test/read-product.webp",
            ProductBaseUnit: "g",
            ProductBaseAmount: 100,
            ProductCaloriesPerBase: 200,
            ProductProteinsPerBase: 10,
            ProductFatsPerBase: 5,
            ProductCarbsPerBase: 25,
            ProductFiberPerBase: 3,
            ProductAlcoholPerBase: 0,
            ProductQualityScore: 82,
            ProductQualityGrade: "good",
            RecipeId: Guid.NewGuid(),
            RecipeName: "Read recipe",
            RecipeImageUrl: "https://example.test/read-recipe.webp",
            RecipeServings: 2,
            RecipeTotalCalories: 400,
            RecipeTotalProteins: 20,
            RecipeTotalFats: 10,
            RecipeTotalCarbs: 50,
            RecipeTotalFiber: 6,
            RecipeTotalAlcohol: 0,
            SourceAiItemId: Guid.NewGuid(),
            Origin: amount == 1 ? "manual" : "ai");

    private static DashboardMealAiSessionReadModel CreateDashboardMealAiSessionReadModel(
        Guid sessionId,
        Guid mealId,
        DateTime recognizedAtUtc,
        IReadOnlyList<DashboardMealAiItemReadModel> items) =>
        new(
            sessionId,
            mealId,
            ImageAssetId: Guid.NewGuid(),
            ImageUrl: "https://example.test/read-ai.webp",
            Source: "Vision",
            Status: "Reviewed",
            recognizedAtUtc,
            Notes: "read notes",
            items);

    private static DashboardMealAiItemReadModel CreateDashboardMealAiItemReadModel(Guid itemId, Guid sessionId, string name) =>
        new(
            itemId,
            sessionId,
            name,
            NameLocal: "local",
            Amount: 1,
            Unit: "portion",
            Calories: 120,
            Proteins: 4,
            Fats: 3,
            Carbs: 20,
            Fiber: 2,
            Alcohol: 0,
            Confidence: 0.9,
            Resolution: "Accepted");
}
