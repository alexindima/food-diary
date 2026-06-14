using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Usda.Models;
using FoodDiary.Application.Fasting.Commands.EndFasting;
using FoodDiary.Application.Fasting.Commands.ExtendActiveFasting;
using FoodDiary.Application.Fasting.Commands.PostponeCyclicDay;
using FoodDiary.Application.Fasting.Commands.ReduceActiveFastingTarget;
using FoodDiary.Application.Fasting.Commands.SkipCyclicDay;
using FoodDiary.Application.Fasting.Commands.StartFasting;
using FoodDiary.Application.Fasting.Commands.UpdateCurrentFastingCheckIn;
using FoodDiary.Application.Fasting.Models;
using FoodDiary.Application.Fasting.Queries.GetFastingInsights;
using FoodDiary.Application.Fasting.Queries.GetFastingStats;
using FoodDiary.Application.Hydration.Commands.CreateHydrationEntry;
using FoodDiary.Application.Hydration.Commands.DeleteHydrationEntry;
using FoodDiary.Application.Hydration.Commands.UpdateHydrationEntry;
using FoodDiary.Application.Hydration.Models;
using FoodDiary.Application.Hydration.Queries.GetHydrationDailyTotal;
using FoodDiary.Application.Hydration.Queries.GetHydrationEntries;
using FoodDiary.Application.ShoppingLists.Commands.CreateShoppingList;
using FoodDiary.Application.ShoppingLists.Commands.DeleteShoppingList;
using FoodDiary.Application.ShoppingLists.Commands.UpdateShoppingList;
using FoodDiary.Application.ShoppingLists.Models;
using FoodDiary.Application.ShoppingLists.Queries.GetCurrentShoppingList;
using FoodDiary.Application.ShoppingLists.Queries.GetShoppingListById;
using FoodDiary.Application.ShoppingLists.Queries.GetShoppingLists;
using FoodDiary.Application.Usda.Commands.LinkProductToUsdaFood;
using FoodDiary.Application.Usda.Commands.UnlinkProductFromUsdaFood;
using FoodDiary.Application.Usda.Queries.GetDailyMicronutrients;
using FoodDiary.Application.Usda.Queries.GetMicronutrients;
using FoodDiary.Application.Usda.Queries.SearchUsdaFoods;
using FoodDiary.Application.WaistEntries.Commands.DeleteWaistEntry;
using FoodDiary.Application.WaistEntries.Commands.CreateWaistEntry;
using FoodDiary.Application.WaistEntries.Commands.UpdateWaistEntry;
using FoodDiary.Application.WaistEntries.Models;
using FoodDiary.Application.WaistEntries.Queries.GetLatestWaistEntry;
using FoodDiary.Application.WaistEntries.Queries.GetWaistEntries;
using FoodDiary.Application.WaistEntries.Queries.GetWaistSummaries;
using FoodDiary.Application.WeeklyCheckIn.Models;
using FoodDiary.Application.WeeklyCheckIn.Queries.GetWeeklyCheckIn;
using FoodDiary.Application.WeightEntries.Commands.DeleteWeightEntry;
using FoodDiary.Application.WeightEntries.Commands.CreateWeightEntry;
using FoodDiary.Application.WeightEntries.Commands.UpdateWeightEntry;
using FoodDiary.Application.WeightEntries.Models;
using FoodDiary.Application.WeightEntries.Queries.GetLatestWeightEntry;
using FoodDiary.Application.WeightEntries.Queries.GetWeightEntries;
using FoodDiary.Application.WeightEntries.Queries.GetWeightSummaries;
using FoodDiary.Mediator;
using FoodDiary.Presentation.Api.Features.Fasting;
using FoodDiary.Presentation.Api.Features.Fasting.Requests;
using FoodDiary.Presentation.Api.Features.Fasting.Responses;
using FoodDiary.Presentation.Api.Features.Hydration;
using FoodDiary.Presentation.Api.Features.Hydration.Requests;
using FoodDiary.Presentation.Api.Features.Hydration.Responses;
using FoodDiary.Presentation.Api.Features.ShoppingLists;
using FoodDiary.Presentation.Api.Features.ShoppingLists.Requests;
using FoodDiary.Presentation.Api.Features.ShoppingLists.Responses;
using FoodDiary.Presentation.Api.Features.Usda;
using FoodDiary.Presentation.Api.Features.Usda.Requests;
using FoodDiary.Presentation.Api.Features.Usda.Responses;
using FoodDiary.Presentation.Api.Features.WaistEntries;
using FoodDiary.Presentation.Api.Features.WaistEntries.Requests;
using FoodDiary.Presentation.Api.Features.WaistEntries.Responses;
using FoodDiary.Presentation.Api.Features.WeeklyCheckIn;
using FoodDiary.Presentation.Api.Features.WeeklyCheckIn.Responses;
using FoodDiary.Presentation.Api.Features.WeightEntries;
using FoodDiary.Presentation.Api.Features.WeightEntries.Requests;
using FoodDiary.Presentation.Api.Features.WeightEntries.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class AdditionalFeatureControllerTests {
    [Fact]
    public async Task ShoppingListsController_CoversRequestedEndpoints() {
        ShoppingListModel list = CreateShoppingList();
        var userId = Guid.NewGuid();
        var listId = Guid.NewGuid();

        CapturedSender allSender = SubstituteSender.Capture(Result.Success<IReadOnlyList<ShoppingListSummaryModel>>([new ShoppingListSummaryModel(listId, "Weekly", DateTime.UtcNow, 2)]));
        ShoppingListsController allController = CreateController(new ShoppingListsController(allSender));
        Assert.IsType<List<ShoppingListSummaryHttpResponse>>(Assert.IsType<OkObjectResult>(await allController.GetAll(userId)).Value);
        Assert.Equal(userId, Assert.IsType<GetShoppingListsQuery>(allSender.Request).UserId);

        CapturedSender currentSender = SubstituteSender.Capture(Result.Success(list));
        ShoppingListsController currentController = CreateController(new ShoppingListsController(currentSender));
        Assert.IsType<ShoppingListHttpResponse>(Assert.IsType<OkObjectResult>(await currentController.GetCurrent(userId)).Value);
        Assert.Equal(userId, Assert.IsType<GetCurrentShoppingListQuery>(currentSender.Request).UserId);

        CapturedSender byIdSender = SubstituteSender.Capture(Result.Success(list));
        ShoppingListsController byIdController = CreateController(new ShoppingListsController(byIdSender));
        Assert.IsType<ShoppingListHttpResponse>(Assert.IsType<OkObjectResult>(await byIdController.GetById(listId, userId)).Value);
        Assert.Equal(listId, Assert.IsType<GetShoppingListByIdQuery>(byIdSender.Request).ShoppingListId);

        CapturedSender createSender = SubstituteSender.Capture(Result.Success(list));
        ShoppingListsController createController = CreateController(new ShoppingListsController(createSender));
        var createRequest = new CreateShoppingListHttpRequest("New", []);
        Assert.IsType<CreatedAtActionResult>(await createController.Create(userId, createRequest));
        Assert.Equal(userId, Assert.IsType<CreateShoppingListCommand>(createSender.Request).UserId);

        CapturedSender updateSender = SubstituteSender.Capture(Result.Success(list));
        ShoppingListsController updateController = CreateController(new ShoppingListsController(updateSender));
        var request = new UpdateShoppingListHttpRequest("Updated", []);
        Assert.IsType<ShoppingListHttpResponse>(Assert.IsType<OkObjectResult>(await updateController.Update(listId, userId, request)).Value);
        Assert.Equal(listId, Assert.IsType<UpdateShoppingListCommand>(updateSender.Request).ShoppingListId);

        CapturedSender deleteSender = SubstituteSender.Capture(Result.Success());
        ShoppingListsController deleteController = CreateController(new ShoppingListsController(deleteSender));
        Assert.IsType<NoContentResult>(await deleteController.Delete(listId, userId));
        Assert.Equal(listId, Assert.IsType<DeleteShoppingListCommand>(deleteSender.Request).ShoppingListId);
    }

    [Fact]
    public async Task UsdaController_CoversAllEndpoints() {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        CapturedSender searchSender = SubstituteSender.Capture(Result.Success<IReadOnlyList<UsdaFoodModel>>([new UsdaFoodModel(1, "Apple", "Fruit")]));
        UsdaController searchController = CreateController(new UsdaController(searchSender));
        IActionResult search = await searchController.Search("apple", 10);
        Assert.IsAssignableFrom<IReadOnlyList<UsdaFoodHttpResponse>>(Assert.IsType<OkObjectResult>(search).Value);
        Assert.Equal("apple", Assert.IsType<SearchUsdaFoodsQuery>(searchSender.Request).Search);

        CapturedSender detailSender = SubstituteSender.Capture(Result.Success(CreateUsdaDetail()));
        UsdaController detailController = CreateController(new UsdaController(detailSender));
        IActionResult detail = await detailController.GetDetail(123);
        Assert.IsType<UsdaFoodDetailHttpResponse>(Assert.IsType<OkObjectResult>(detail).Value);
        Assert.Equal(123, Assert.IsType<GetMicronutrientsQuery>(detailSender.Request).FdcId);

        CapturedSender linkSender = SubstituteSender.Capture(Result.Success());
        UsdaController linkController = CreateController(new UsdaController(linkSender));
        Assert.IsType<NoContentResult>(await linkController.LinkProduct(userId, productId, new LinkProductToUsdaFoodHttpRequest(123)));
        Assert.Equal(123, Assert.IsType<LinkProductToUsdaFoodCommand>(linkSender.Request).FdcId);

        CapturedSender unlinkSender = SubstituteSender.Capture(Result.Success());
        UsdaController unlinkController = CreateController(new UsdaController(unlinkSender));
        Assert.IsType<NoContentResult>(await unlinkController.UnlinkProduct(userId, productId));
        Assert.Equal(productId, Assert.IsType<UnlinkProductFromUsdaFoodCommand>(unlinkSender.Request).ProductId);

        DateTime date = DateTime.UtcNow.Date;
        CapturedSender dailySender = SubstituteSender.Capture(Result.Success(new DailyMicronutrientSummaryModel(date, 1, 2, [], HealthScores: null)));
        UsdaController dailyController = CreateController(new UsdaController(dailySender));
        Assert.IsType<DailyMicronutrientSummaryHttpResponse>(Assert.IsType<OkObjectResult>(await dailyController.GetDailyMicronutrients(userId, date)).Value);
        Assert.Equal(date, Assert.IsType<GetDailyMicronutrientsQuery>(dailySender.Request).Date);
    }

    [Fact]
    public async Task HydrationEntriesController_CoversRequestedEndpoints() {
        var userId = Guid.NewGuid();
        var entryId = Guid.NewGuid();
        DateTime now = new(2026, 6, 14, 8, 0, 0, DateTimeKind.Utc);
        var model = new HydrationEntryModel(entryId, now, 500);

        CapturedSender listSender = SubstituteSender.Capture(Result.Success<IReadOnlyList<HydrationEntryModel>>([model]));
        HydrationEntriesController listController = CreateHydrationController(listSender, now);
        Assert.IsType<List<HydrationEntryHttpResponse>>(Assert.IsType<OkObjectResult>(await listController.GetByDate(userId, new GetHydrationEntriesHttpQuery())).Value);
        Assert.Equal(now, Assert.IsType<GetHydrationEntriesQuery>(listSender.Request).DateUtc);

        CapturedSender dailySender = SubstituteSender.Capture(Result.Success(new HydrationDailyModel(now.Date, TotalMl: 500, GoalMl: 2000)));
        HydrationEntriesController dailyController = CreateHydrationController(dailySender, now);
        Assert.IsType<HydrationDailyHttpResponse>(Assert.IsType<OkObjectResult>(await dailyController.GetDaily(userId, new GetHydrationEntriesHttpQuery(now.Date))).Value);
        Assert.Equal(now.Date, Assert.IsType<GetHydrationDailyTotalQuery>(dailySender.Request).DateUtc);

        CapturedSender createSender = SubstituteSender.Capture(Result.Success(model));
        HydrationEntriesController createController = CreateHydrationController(createSender, now);
        Assert.IsType<HydrationEntryHttpResponse>(Assert.IsType<OkObjectResult>(await createController.Create(userId, new CreateHydrationEntryHttpRequest(now, 250))).Value);
        Assert.Equal(250, Assert.IsType<CreateHydrationEntryCommand>(createSender.Request).AmountMl);

        CapturedSender updateSender = SubstituteSender.Capture(Result.Success(model));
        HydrationEntriesController updateController = CreateHydrationController(updateSender, now);
        Assert.IsType<HydrationEntryHttpResponse>(Assert.IsType<OkObjectResult>(await updateController.Update(entryId, userId, new UpdateHydrationEntryHttpRequest(now, 750))).Value);
        Assert.Equal(entryId, Assert.IsType<UpdateHydrationEntryCommand>(updateSender.Request).HydrationEntryId);

        CapturedSender deleteSender = SubstituteSender.Capture(Result.Success());
        HydrationEntriesController deleteController = CreateHydrationController(deleteSender, now);
        Assert.IsType<NoContentResult>(await deleteController.Delete(entryId, userId));
        Assert.Equal(entryId, Assert.IsType<DeleteHydrationEntryCommand>(deleteSender.Request).HydrationEntryId);
    }

    [Fact]
    public async Task WeightEntriesController_CoversRequestedEndpoints() {
        var userId = Guid.NewGuid();
        var entryId = Guid.NewGuid();
        DateTime date = DateTime.UtcNow.Date;
        var model = new WeightEntryModel(entryId, userId, date, 75.5);
        var summary = new WeightEntrySummaryModel(date.AddDays(-7), date, 75.2);

        CapturedSender allSender = SubstituteSender.Capture(Result.Success<IReadOnlyList<WeightEntryModel>>([model]));
        WeightEntriesController allController = CreateController(new WeightEntriesController(allSender));
        Assert.IsType<List<WeightEntryHttpResponse>>(Assert.IsType<OkObjectResult>(await allController.GetAll(userId, new GetWeightEntriesHttpQuery())).Value);
        Assert.IsType<GetWeightEntriesQuery>(allSender.Request);

        CapturedSender latestSender = SubstituteSender.Capture(Result.Success<WeightEntryModel?>(model));
        WeightEntriesController latestController = CreateController(new WeightEntriesController(latestSender));
        Assert.IsType<WeightEntryHttpResponse>(Assert.IsType<OkObjectResult>(await latestController.GetLatest(userId)).Value);
        Assert.IsType<GetLatestWeightEntryQuery>(latestSender.Request);

        CapturedSender nullLatestSender = SubstituteSender.Capture(Result.Success<WeightEntryModel?>(value: null));
        WeightEntriesController nullLatestController = CreateController(new WeightEntriesController(nullLatestSender));
        Assert.Null(Assert.IsType<OkObjectResult>(await nullLatestController.GetLatest(userId)).Value);

        CapturedSender summarySender = SubstituteSender.Capture(Result.Success<IReadOnlyList<WeightEntrySummaryModel>>([summary]));
        WeightEntriesController summaryController = CreateController(new WeightEntriesController(summarySender));
        Assert.IsType<List<WeightEntrySummaryHttpResponse>>(Assert.IsType<OkObjectResult>(await summaryController.GetSummary(userId, new GetWeightSummariesHttpQuery(date.AddDays(-7), date))).Value);
        Assert.IsType<GetWeightSummariesQuery>(summarySender.Request);

        CapturedSender createSender = SubstituteSender.Capture(Result.Success(model));
        WeightEntriesController createController = CreateController(new WeightEntriesController(createSender));
        Assert.IsType<CreatedResult>(await createController.Create(userId, new CreateWeightEntryHttpRequest(date, 75.5)));
        Assert.Equal(userId, Assert.IsType<CreateWeightEntryCommand>(createSender.Request).UserId);

        CapturedSender updateSender = SubstituteSender.Capture(Result.Success(model));
        WeightEntriesController updateController = CreateController(new WeightEntriesController(updateSender));
        Assert.IsType<WeightEntryHttpResponse>(Assert.IsType<OkObjectResult>(await updateController.Update(entryId, userId, new UpdateWeightEntryHttpRequest(date, 74.8))).Value);
        Assert.Equal(entryId, Assert.IsType<UpdateWeightEntryCommand>(updateSender.Request).WeightEntryId);

        CapturedSender deleteSender = SubstituteSender.Capture(Result.Success());
        WeightEntriesController deleteController = CreateController(new WeightEntriesController(deleteSender));
        Assert.IsType<NoContentResult>(await deleteController.Delete(entryId, userId));
        Assert.Equal(entryId, Assert.IsType<DeleteWeightEntryCommand>(deleteSender.Request).WeightEntryId);
    }

    [Fact]
    public async Task WaistEntriesController_CoversRequestedEndpoints() {
        var userId = Guid.NewGuid();
        var entryId = Guid.NewGuid();
        DateTime date = DateTime.UtcNow.Date;
        var model = new WaistEntryModel(entryId, userId, date, 80.5);
        var summary = new WaistEntrySummaryModel(date.AddDays(-7), date, 80.2);

        CapturedSender allSender = SubstituteSender.Capture(Result.Success<IReadOnlyList<WaistEntryModel>>([model]));
        WaistEntriesController allController = CreateController(new WaistEntriesController(allSender));
        Assert.IsType<List<WaistEntryHttpResponse>>(Assert.IsType<OkObjectResult>(await allController.GetAll(userId, new GetWaistEntriesHttpQuery())).Value);
        Assert.IsType<GetWaistEntriesQuery>(allSender.Request);

        CapturedSender latestSender = SubstituteSender.Capture(Result.Success<WaistEntryModel?>(model));
        WaistEntriesController latestController = CreateController(new WaistEntriesController(latestSender));
        Assert.IsType<WaistEntryHttpResponse>(Assert.IsType<OkObjectResult>(await latestController.GetLatest(userId)).Value);
        Assert.IsType<GetLatestWaistEntryQuery>(latestSender.Request);

        CapturedSender nullLatestSender = SubstituteSender.Capture(Result.Success<WaistEntryModel?>(value: null));
        WaistEntriesController nullLatestController = CreateController(new WaistEntriesController(nullLatestSender));
        Assert.Null(Assert.IsType<OkObjectResult>(await nullLatestController.GetLatest(userId)).Value);

        CapturedSender summarySender = SubstituteSender.Capture(Result.Success<IReadOnlyList<WaistEntrySummaryModel>>([summary]));
        WaistEntriesController summaryController = CreateController(new WaistEntriesController(summarySender));
        Assert.IsType<List<WaistEntrySummaryHttpResponse>>(Assert.IsType<OkObjectResult>(await summaryController.GetSummary(userId, new GetWaistSummariesHttpQuery(date.AddDays(-7), date))).Value);
        Assert.IsType<GetWaistSummariesQuery>(summarySender.Request);

        CapturedSender createSender = SubstituteSender.Capture(Result.Success(model));
        WaistEntriesController createController = CreateController(new WaistEntriesController(createSender));
        Assert.IsType<WaistEntryHttpResponse>(Assert.IsType<OkObjectResult>(await createController.Create(userId, new CreateWaistEntryHttpRequest(date, 80.5))).Value);
        Assert.Equal(userId, Assert.IsType<CreateWaistEntryCommand>(createSender.Request).UserId);

        CapturedSender updateSender = SubstituteSender.Capture(Result.Success(model));
        WaistEntriesController updateController = CreateController(new WaistEntriesController(updateSender));
        Assert.IsType<WaistEntryHttpResponse>(Assert.IsType<OkObjectResult>(await updateController.Update(entryId, userId, new UpdateWaistEntryHttpRequest(date, 79.8))).Value);
        Assert.Equal(entryId, Assert.IsType<UpdateWaistEntryCommand>(updateSender.Request).WaistEntryId);

        CapturedSender deleteSender = SubstituteSender.Capture(Result.Success());
        WaistEntriesController deleteController = CreateController(new WaistEntriesController(deleteSender));
        Assert.IsType<NoContentResult>(await deleteController.Delete(entryId, userId));
        Assert.Equal(entryId, Assert.IsType<DeleteWaistEntryCommand>(deleteSender.Request).WaistEntryId);
    }

    [Fact]
    public async Task WeeklyCheckInController_Get_SendsQueryAndReturnsResponse() {
        var userId = Guid.NewGuid();
        var summary = new WeekSummaryModel(1, 1, 1, 1, 1, 1, 1, WeightStart: null, WeightEnd: null, WaistStart: null, WaistEnd: null, 1000, 1000);
        var model = new WeeklyCheckInModel(summary, summary, new WeekTrendModel(0, 0, 0, 0, WeightChange: null, WaistChange: null, 0, 0), []);
        CapturedSender sender = SubstituteSender.Capture(Result.Success(model));
        WeeklyCheckInController controller = CreateController(new WeeklyCheckInController(sender));

        IActionResult result = await controller.Get(userId);

        Assert.IsType<WeeklyCheckInHttpResponse>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal(userId, Assert.IsType<GetWeeklyCheckInQuery>(sender.Request).UserId);
    }

    [Fact]
    public async Task FastingController_CoversRequestedEndpoints() {
        var userId = Guid.NewGuid();
        FastingSessionModel session = CreateFastingSession();

        CapturedSender startSender = SubstituteSender.Capture(Result.Success(session));
        FastingController startController = CreateController(new FastingController(startSender));
        Assert.IsType<FastingSessionHttpResponse>(Assert.IsType<OkObjectResult>(await startController.Start(userId, new StartFastingHttpRequest("F16_8"))).Value);
        Assert.IsType<StartFastingCommand>(startSender.Request);

        CapturedSender endSender = SubstituteSender.Capture(Result.Success(session));
        FastingController endController = CreateController(new FastingController(endSender));
        Assert.IsType<FastingSessionHttpResponse>(Assert.IsType<OkObjectResult>(await endController.End(userId)).Value);
        Assert.IsType<EndFastingCommand>(endSender.Request);

        CapturedSender extendSender = SubstituteSender.Capture(Result.Success(session));
        FastingController extendController = CreateController(new FastingController(extendSender));
        Assert.IsType<FastingSessionHttpResponse>(Assert.IsType<OkObjectResult>(await extendController.ExtendDuration(userId, new ExtendActiveFastingHttpRequest(2))).Value);
        Assert.Equal(2, Assert.IsType<ExtendActiveFastingCommand>(extendSender.Request).AdditionalHours);

        CapturedSender reduceSender = SubstituteSender.Capture(Result.Success(session));
        FastingController reduceController = CreateController(new FastingController(reduceSender));
        Assert.IsType<FastingSessionHttpResponse>(Assert.IsType<OkObjectResult>(await reduceController.ReduceDuration(userId, new ReduceActiveFastingTargetHttpRequest(1))).Value);
        Assert.Equal(1, Assert.IsType<ReduceActiveFastingTargetCommand>(reduceSender.Request).ReducedHours);

        CapturedSender checkInSender = SubstituteSender.Capture(Result.Success(session));
        FastingController checkInController = CreateController(new FastingController(checkInSender));
        Assert.IsType<FastingSessionHttpResponse>(Assert.IsType<OkObjectResult>(await checkInController.UpdateCheckIn(userId, new UpdateFastingCheckInHttpRequest(1, 2, 3, ["headache"], "ok"))).Value);
        Assert.Equal(3, Assert.IsType<UpdateCurrentFastingCheckInCommand>(checkInSender.Request).MoodLevel);

        CapturedSender skipSender = SubstituteSender.Capture(Result.Success(session));
        FastingController skipController = CreateController(new FastingController(skipSender));
        Assert.IsType<FastingSessionHttpResponse>(Assert.IsType<OkObjectResult>(await skipController.SkipCyclicDay(userId)).Value);
        Assert.IsType<SkipCyclicDayCommand>(skipSender.Request);

        CapturedSender postponeSender = SubstituteSender.Capture(Result.Success(session));
        FastingController postponeController = CreateController(new FastingController(postponeSender));
        Assert.IsType<FastingSessionHttpResponse>(Assert.IsType<OkObjectResult>(await postponeController.PostponeCyclicDay(userId)).Value);
        Assert.IsType<PostponeCyclicDayCommand>(postponeSender.Request);
    }

    [Fact]
    public async Task FastingInsightsController_CoversEndpoints() {
        var userId = Guid.NewGuid();
        var stats = new FastingStatsModel(5, 2, 18, 80, 60, DateTime.UtcNow, "tired");
        CapturedSender statsSender = SubstituteSender.Capture(Result.Success(stats));
        FastingInsightsController statsController = CreateController(new FastingInsightsController(statsSender));
        Assert.IsType<FastingStatsHttpResponse>(Assert.IsType<OkObjectResult>(await statsController.GetStats(userId)).Value);
        Assert.IsType<GetFastingStatsQuery>(statsSender.Request);

        var insights = new FastingInsightsModel([new FastingMessageModel("a", "title", "body", "neutral")], []);
        CapturedSender insightsSender = SubstituteSender.Capture(Result.Success(insights));
        FastingInsightsController insightsController = CreateController(new FastingInsightsController(insightsSender));
        Assert.IsType<FastingInsightsHttpResponse>(Assert.IsType<OkObjectResult>(await insightsController.GetInsights(userId)).Value);
        Assert.IsType<GetFastingInsightsQuery>(insightsSender.Request);
    }

    private static TController CreateController<TController>(TController controller)
        where TController : ControllerBase {
        controller.ControllerContext = new ControllerContext {
            HttpContext = new DefaultHttpContext(),
        };
        return controller;
    }

    private static HydrationEntriesController CreateHydrationController(ISender sender, DateTime utcNow) =>
        CreateController(new HydrationEntriesController(sender, new FixedTimeProvider(utcNow)));

    private static ShoppingListModel CreateShoppingList() =>
        new(Guid.NewGuid(), "Weekly", DateTime.UtcNow, []);

    private static UsdaFoodDetailModel CreateUsdaDetail() =>
        new(
            123,
            "Broccoli",
            "Vegetable",
            [new MicronutrientModel(1, "Vitamin C", "mg", 50, 90, 55.5)],
            [new UsdaFoodPortionModel(1, 1, "cup", 150, "1 cup", Modifier: null)],
            HealthScores: null);

    private static FastingSessionModel CreateFastingSession() =>
        new(
            Guid.NewGuid(),
            DateTime.UtcNow.AddHours(-1),
            EndedAtUtc: null,
            InitialPlannedDurationHours: 16,
            AddedDurationHours: 0,
            PlannedDurationHours: 16,
            Protocol: "F16_8",
            PlanType: "Intermittent",
            OccurrenceKind: "FastingWindow",
            CyclicFastDays: null,
            CyclicEatDays: null,
            CyclicEatDayFastHours: null,
            CyclicEatDayEatingWindowHours: null,
            CyclicPhaseDayNumber: null,
            CyclicPhaseDayTotal: null,
            IsCompleted: false,
            Status: "Active",
            Notes: null,
            CheckInAtUtc: null,
            HungerLevel: null,
            EnergyLevel: null,
            MoodLevel: null,
            Symptoms: [],
            CheckInNotes: null,
            CheckIns: []);

    [ExcludeFromCodeCoverage]
    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }
}
