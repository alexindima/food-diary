using FoodDiary.Results;
using FoodDiary.Application.Cycles.Commands.ClearCycleDay;
using FoodDiary.Application.Cycles.Commands.CreateCycle;
using FoodDiary.Application.Cycles.Commands.UpsertCycleFactor;
using FoodDiary.Application.Cycles.Commands.UpsertCycleDay;
using FoodDiary.Application.Cycles.Models;
using FoodDiary.Application.Cycles.Queries.GetCycleNutritionSummary;
using FoodDiary.Application.Cycles.Queries.GetCurrentCycle;
using FoodDiary.Domain.Enums;
using FoodDiary.Mediator;
using FoodDiary.Presentation.Api.Features.Cycles;
using FoodDiary.Presentation.Api.Features.Cycles.Requests;
using FoodDiary.Presentation.Api.Features.Cycles.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class CyclesControllerCoverageTests {
    [Fact]
    public async Task GetCurrent_ReturnsCycleAndSendsQuery() {
        var userId = Guid.NewGuid();
        var cycleProfileId = Guid.NewGuid();
        IRequest<Result<CycleModel?>>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success<CycleModel?>(CreateCycle(cycleProfileId, userId)), request => sentRequest = request);
        CyclesController controller = CreateController(new CyclesController(sender));

        IActionResult result = await controller.GetCurrent(userId);

        Assert.IsType<CycleHttpResponse>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal(userId, Assert.IsType<GetCurrentCycleQuery>(sentRequest).UserId);
    }

    [Fact]
    public async Task GetCurrent_ReturnsNullWhenNoCycle() {
        var userId = Guid.NewGuid();
        ISender sender = SubstituteSender.Create(Result.Success<CycleModel?>(value: null));
        CyclesController controller = CreateController(new CyclesController(sender));

        IActionResult result = await controller.GetCurrent(userId);

        Assert.Null(Assert.IsType<OkObjectResult>(result).Value);
    }

    [Fact]
    public async Task GetNutritionSummary_ReturnsSummaryAndSendsQuery() {
        var userId = Guid.NewGuid();
        DateTime dateFrom = new(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime dateTo = dateFrom.AddDays(7);
        IRequest<Result<CycleNutritionSummaryModel?>>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success<CycleNutritionSummaryModel?>(CreateNutritionSummary(dateFrom, dateTo)), request => sentRequest = request);
        CyclesController controller = CreateController(new CyclesController(sender));

        IActionResult result = await controller.GetNutritionSummary(userId, dateFrom, dateTo);

        Assert.IsType<CycleNutritionSummaryHttpResponse>(Assert.IsType<OkObjectResult>(result).Value);
        GetCycleNutritionSummaryQuery query = Assert.IsType<GetCycleNutritionSummaryQuery>(sentRequest);
        Assert.Equal(userId, query.UserId);
        Assert.Equal(dateFrom, query.DateFrom);
        Assert.Equal(dateTo, query.DateTo);
    }

    [Fact]
    public async Task GetNutritionSummary_ReturnsNullWhenNoSummary() {
        var userId = Guid.NewGuid();
        DateTime dateFrom = new(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        ISender sender = SubstituteSender.Create(Result.Success<CycleNutritionSummaryModel?>(value: null));
        CyclesController controller = CreateController(new CyclesController(sender));

        IActionResult result = await controller.GetNutritionSummary(userId, dateFrom, dateFrom.AddDays(7));

        Assert.Null(Assert.IsType<OkObjectResult>(result).Value);
    }

    [Fact]
    public async Task Create_SendsCommandAndReturnsCycle() {
        var userId = Guid.NewGuid();
        var cycleProfileId = Guid.NewGuid();
        DateTime trackingStartDate = new(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        IRequest<Result<CycleModel>>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success(CreateCycle(cycleProfileId, userId)), request => sentRequest = request);
        CyclesController controller = CreateController(new CyclesController(sender));
        var request = new CreateCycleHttpRequest(
            trackingStartDate,
            (int)CycleTrackingMode.TryingToConceive,
            AverageCycleLength: 30,
            AveragePeriodLength: 6,
            LutealLength: 12,
            IsRegular: true,
            IsOnboardingComplete: true,
            ShowFertilityEstimates: true,
            DiscreetNotifications: false,
            Notes: "notes");

        IActionResult result = await controller.Create(userId, request);

        Assert.IsType<CycleHttpResponse>(Assert.IsType<OkObjectResult>(result).Value);
        CreateCycleCommand command = Assert.IsType<CreateCycleCommand>(sentRequest);
        Assert.Equal(userId, command.UserId);
        Assert.Equal(trackingStartDate, command.TrackingStartDate);
        Assert.Equal((int)CycleTrackingMode.TryingToConceive, command.Mode);
        Assert.Equal("notes", command.Notes);
    }

    [Fact]
    public async Task UpsertDay_SendsCommandAndReturnsLogDay() {
        var userId = Guid.NewGuid();
        var cycleProfileId = Guid.NewGuid();
        DateTime date = new(2026, 4, 3, 0, 0, 0, DateTimeKind.Utc);
        IRequest<Result<CycleLogDayModel>>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success(CreateLogDay(cycleProfileId, date)), request => sentRequest = request);
        CyclesController controller = CreateController(new CyclesController(sender));

        IActionResult result = await controller.UpsertDay(cycleProfileId, userId, CreateUpsertDayRequest(date));

        Assert.IsType<CycleLogDayHttpResponse>(Assert.IsType<OkObjectResult>(result).Value);
        UpsertCycleDayCommand command = Assert.IsType<UpsertCycleDayCommand>(sentRequest);
        Assert.Equal(userId, command.UserId);
        Assert.Equal(cycleProfileId, command.CycleProfileId);
        Assert.Equal(date, command.Date);
        Assert.NotNull(command.Bleeding);
        Assert.Single(command.Symptoms);
        Assert.NotNull(command.FertilitySignal);
    }

    [Fact]
    public async Task ClearDay_SendsCommandAndReturnsNoContent() {
        var userId = Guid.NewGuid();
        var cycleProfileId = Guid.NewGuid();
        DateTime date = new(2026, 4, 3, 0, 0, 0, DateTimeKind.Utc);
        IRequest<Result>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success(), request => sentRequest = request);
        CyclesController controller = CreateController(new CyclesController(sender));

        IActionResult result = await controller.ClearDay(cycleProfileId, userId, date);

        Assert.IsType<NoContentResult>(result);
        ClearCycleDayCommand command = Assert.IsType<ClearCycleDayCommand>(sentRequest);
        Assert.Equal(userId, command.UserId);
        Assert.Equal(cycleProfileId, command.CycleProfileId);
        Assert.Equal(date, command.Date);
    }

    [Fact]
    public async Task UpsertFactor_SendsCommandAndReturnsCycle() {
        var userId = Guid.NewGuid();
        var cycleProfileId = Guid.NewGuid();
        DateTime startDate = new(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        IRequest<Result<CycleModel>>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success(CreateCycle(cycleProfileId, userId)), request => sentRequest = request);
        CyclesController controller = CreateController(new CyclesController(sender));
        var request = new UpsertCycleFactorHttpRequest(
            (int)CycleFactorType.HormonalContraception,
            startDate,
            EndDate: null,
            Notes: "notes",
            ClearNotes: false);

        IActionResult result = await controller.UpsertFactor(cycleProfileId, userId, request);

        Assert.IsType<CycleHttpResponse>(Assert.IsType<OkObjectResult>(result).Value);
        UpsertCycleFactorCommand command = Assert.IsType<UpsertCycleFactorCommand>(sentRequest);
        Assert.Equal(userId, command.UserId);
        Assert.Equal(cycleProfileId, command.CycleProfileId);
        Assert.Equal((int)CycleFactorType.HormonalContraception, command.Type);
        Assert.Equal(startDate, command.StartDate);
        Assert.Equal("notes", command.Notes);
    }

    private static TController CreateController<TController>(TController controller)
        where TController : ControllerBase {
        controller.ControllerContext = new ControllerContext {
            HttpContext = new DefaultHttpContext(),
        };
        return controller;
    }

    private static CycleNutritionSummaryModel CreateNutritionSummary(DateTime dateFrom, DateTime dateTo) =>
        new(
            dateFrom,
            dateTo,
            LoggedCycleDays: 4,
            DaysWithMeals: 3,
            BleedingDays: 2,
            AverageCaloriesOnBleedingDays: 2100,
            AverageCaloriesOnNonBleedingCycleDays: 1800,
            AverageFiberOnBleedingDays: 18,
            AverageFiberOnNonBleedingCycleDays: 28,
            AveragePainImpactOnDaysWithMeals: 6.5,
            HasEnoughNutritionData: true);

    private static UpsertCycleDayHttpRequest CreateUpsertDayRequest(DateTime date) =>
        new(
            date,
            new BleedingLogHttpModel((int)BleedingType.Bleeding, (int)CycleFlowLevel.Medium, PainImpact: 2, Notes: "notes", ClearNotes: false),
            [new SymptomLogHttpModel((int)CycleSymptomCategory.Pain, Intensity: 4, Tags: ["cramp"], Note: "note", ClearNote: false)],
            new FertilitySignalHttpModel(36.7, (int)OvulationTestResult.Positive, "egg-white", HadSex: true, Notes: "peak", ClearNotes: false));

    private static CycleLogDayModel CreateLogDay(Guid cycleProfileId, DateTime date) =>
        new(
            cycleProfileId,
            date,
            [
                new BleedingEntryModel(
                    Guid.NewGuid(),
                    cycleProfileId,
                    date,
                    BleedingType.Bleeding,
                    CycleFlowLevel.Medium,
                    PainImpact: 2,
                    Notes: "notes"),
            ],
            [
                new CycleSymptomEntryModel(
                    Guid.NewGuid(),
                    cycleProfileId,
                    date,
                    CycleSymptomCategory.Pain,
                    Intensity: 4,
                    Tags: ["cramp"],
                    Note: "note"),
            ],
            new FertilitySignalModel(
                Guid.NewGuid(),
                cycleProfileId,
                date,
                BasalBodyTemperatureCelsius: 36.7,
                OvulationTestResult: OvulationTestResult.Positive,
                CervicalFluid: "egg-white",
                HadSex: true,
                Notes: "peak"));

    private static CycleModel CreateCycle(Guid cycleProfileId, Guid userId) =>
        new(
            cycleProfileId,
            userId,
            CycleTrackingMode.PeriodTracking,
            CycleConfidence.Learning,
            DateTime.UtcNow.Date,
            AverageCycleLength: 28,
            AveragePeriodLength: 5,
            LutealLength: 14,
            IsRegular: true,
            IsOnboardingComplete: true,
            ShowFertilityEstimates: true,
            DiscreetNotifications: false,
            Notes: null,
            BleedingEntries: [],
            Symptoms: [],
            Factors: [],
            FertilitySignals: [],
            Predictions: null);
}
