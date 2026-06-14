using FoodDiary.Application.Cycles.Commands.ClearCycleDay;
using FoodDiary.Application.Cycles.Commands.CreateCycle;
using FoodDiary.Application.Cycles.Commands.UpsertCycleFactor;
using FoodDiary.Application.Cycles.Commands.UpsertCycleDay;
using FoodDiary.Application.Cycles.Models;
using FoodDiary.Application.Cycles.Queries.GetCycleNutritionSummary;
using FoodDiary.Application.Cycles.Queries.GetCurrentCycle;
using FoodDiary.Domain.Enums;
using FoodDiary.Presentation.Api.Features.Cycles.Mappings;
using FoodDiary.Presentation.Api.Features.Cycles.Requests;
using FoodDiary.Presentation.Api.Features.Cycles.Responses;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class CycleHttpMappingsTests {
    [Fact]
    public void CreateCycleRequest_ToCommand_MapsAllFields() {
        var userId = Guid.NewGuid();
        DateTime trackingStartDate = new(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
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

        CreateCycleCommand command = request.ToCommand(userId);
        GetCurrentCycleQuery currentQuery = userId.ToCurrentQuery();

        Assert.Equal(userId, command.UserId);
        Assert.Equal(userId, currentQuery.UserId);
        Assert.Equal(trackingStartDate, command.TrackingStartDate);
        Assert.Equal((int)CycleTrackingMode.TryingToConceive, command.Mode);
        Assert.Equal(30, command.AverageCycleLength);
        Assert.Equal(6, command.AveragePeriodLength);
        Assert.Equal(12, command.LutealLength);
        Assert.True(command.IsRegular);
        Assert.True(command.IsOnboardingComplete);
        Assert.True(command.ShowFertilityEstimates);
        Assert.False(command.DiscreetNotifications);
        Assert.Equal("notes", command.Notes);
    }

    [Fact]
    public void UpsertCycleDayRequest_ToCommand_MapsClearNotes() {
        var userId = Guid.NewGuid();
        var cycleProfileId = Guid.NewGuid();
        var request = new UpsertCycleDayHttpRequest(
            Date: new DateTime(2026, 3, 27, 0, 0, 0, DateTimeKind.Utc),
            Bleeding: new BleedingLogHttpModel((int)BleedingType.Bleeding, (int)CycleFlowLevel.Medium, PainImpact: 2, Notes: null, ClearNotes: true),
            Symptoms: [new SymptomLogHttpModel((int)CycleSymptomCategory.Pain, 4, ["cramp"], Note: null, ClearNote: false)],
            FertilitySignal: new FertilitySignalHttpModel(36.7, (int)OvulationTestResult.Positive, "egg-white", HadSex: true, Notes: "peak", ClearNotes: false));

        UpsertCycleDayCommand command = request.ToCommand(userId, cycleProfileId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal(cycleProfileId, command.CycleProfileId);
        Assert.Equal(request.Date, command.Date);
        BleedingLogHttpModel bleeding = request.Bleeding!;
        Assert.Equal(bleeding.Type, command.Bleeding!.Type);
        Assert.Equal(bleeding.ClearNotes, command.Bleeding.ClearNotes);
        Assert.Single(command.Symptoms);
        FertilitySignalHttpModel fertilitySignal = request.FertilitySignal!;
        Assert.Equal(fertilitySignal.BasalBodyTemperatureCelsius, command.FertilitySignal!.BasalBodyTemperatureCelsius);
        Assert.Equal(fertilitySignal.OvulationTestResult, command.FertilitySignal.OvulationTestResult);
        Assert.Equal(fertilitySignal.CervicalFluid, command.FertilitySignal.CervicalFluid);
        Assert.Equal(fertilitySignal.HadSex, command.FertilitySignal.HadSex);
        Assert.Equal(fertilitySignal.Notes, command.FertilitySignal.Notes);
        Assert.Equal(fertilitySignal.ClearNotes, command.FertilitySignal.ClearNotes);
    }

    [Fact]
    public void UpsertCycleFactorRequest_ToCommand_MapsClearNotes() {
        var userId = Guid.NewGuid();
        var cycleProfileId = Guid.NewGuid();
        var request = new UpsertCycleFactorHttpRequest(
            Type: (int)CycleFactorType.HormonalContraception,
            StartDate: new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate: null,
            Notes: null,
            ClearNotes: true);

        UpsertCycleFactorCommand command = request.ToCommand(userId, cycleProfileId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal(cycleProfileId, command.CycleProfileId);
        Assert.Equal(request.Type, command.Type);
        Assert.Equal(request.StartDate, command.StartDate);
        Assert.True(command.ClearNotes);
    }

    [Fact]
    public void CycleProfileId_ToClearDayCommand_MapsUserIdProfileIdAndDate() {
        var userId = Guid.NewGuid();
        var cycleProfileId = Guid.NewGuid();
        DateTime date = new(2026, 4, 2, 0, 0, 0, DateTimeKind.Utc);

        ClearCycleDayCommand command = cycleProfileId.ToClearDayCommand(userId, date);

        Assert.Equal(userId, command.UserId);
        Assert.Equal(cycleProfileId, command.CycleProfileId);
        Assert.Equal(date, command.Date);
    }

    [Fact]
    public void NutritionSummaryQuery_ToQuery_MapsRange() {
        var userId = Guid.NewGuid();
        DateTime dateFrom = new(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime dateTo = dateFrom.AddDays(7);

        GetCycleNutritionSummaryQuery query = userId.ToNutritionSummaryQuery(dateFrom, dateTo);

        Assert.Equal(userId, query.UserId);
        Assert.Equal(dateFrom, query.DateFrom);
        Assert.Equal(dateTo, query.DateTo);
    }

    [Fact]
    public void CycleModel_ToHttpResponse_MapsDaysAndPredictions() {
        var cycleId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var bleedingId = Guid.NewGuid();
        var startDate = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        var model = new CycleModel(
            cycleId,
            userId,
            CycleTrackingMode.PeriodTracking,
            CycleConfidence.Medium,
            startDate,
            AverageCycleLength: 29,
            AveragePeriodLength: 5,
            LutealLength: 13,
            IsRegular: true,
            IsOnboardingComplete: true,
            ShowFertilityEstimates: true,
            DiscreetNotifications: true,
            Notes: "cycle notes",
            [
                new BleedingEntryModel(
                    bleedingId,
                    cycleId,
                    startDate.AddDays(1),
                    BleedingType.Bleeding,
                    CycleFlowLevel.Medium,
                    PainImpact: 3,
                    Notes: "day notes"),
            ],
            Symptoms: [],
            Factors: [],
            FertilitySignals: [],
            new CyclePredictionsModel(
                startDate.AddDays(28),
                startDate.AddDays(30),
                startDate.AddDays(14),
                startDate.AddDays(16),
                startDate.AddDays(24),
                startDate.AddDays(30),
                "Medium",
                "test"));

        CycleHttpResponse response = model.ToHttpResponse();

        Assert.Equal(cycleId, response.Id);
        Assert.Equal(userId, response.UserId);
        Assert.Equal(startDate, response.TrackingStartDate);
        Assert.Equal(29, response.AverageCycleLength);
        Assert.Equal(13, response.LutealLength);
        Assert.Equal("cycle notes", response.Notes);
        BleedingEntryHttpResponse day = Assert.Single(response.BleedingEntries);
        Assert.Equal(bleedingId, day.Id);
        Assert.Equal((int)CycleFlowLevel.Medium, day.Flow);
        Assert.Equal("day notes", day.Notes);
        Assert.Equal(startDate.AddDays(28), response.Predictions!.NextPeriodStartFrom);
        Assert.Equal(startDate.AddDays(16), response.Predictions.OvulationTo);
        Assert.Equal("Medium", response.Predictions.Confidence);
    }

    [Fact]
    public void CycleModel_ToHttpResponse_MapsNullPredictions() {
        var model = new CycleModel(
            Guid.NewGuid(),
            Guid.NewGuid(),
            CycleTrackingMode.PeriodTracking,
            CycleConfidence.Learning,
            DateTime.UtcNow,
            AverageCycleLength: 28,
            AveragePeriodLength: 5,
            LutealLength: 14,
            IsRegular: false,
            IsOnboardingComplete: false,
            ShowFertilityEstimates: false,
            DiscreetNotifications: true,
            Notes: null,
            [],
            [],
            [],
            [],
            Predictions: null);

        CycleHttpResponse response = model.ToHttpResponse();

        Assert.Null(response.Predictions);
        Assert.Empty(response.BleedingEntries);
    }

    [Fact]
    public void CycleNutritionSummary_ToHttpResponse_MapsAllFields() {
        DateTime dateFrom = new(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        var model = new CycleNutritionSummaryModel(
            dateFrom,
            dateFrom.AddDays(7),
            LoggedCycleDays: 4,
            DaysWithMeals: 3,
            BleedingDays: 2,
            AverageCaloriesOnBleedingDays: 2100,
            AverageCaloriesOnNonBleedingCycleDays: 1800,
            AverageFiberOnBleedingDays: 18,
            AverageFiberOnNonBleedingCycleDays: 28,
            AveragePainImpactOnDaysWithMeals: 6.5,
            HasEnoughNutritionData: true);

        CycleNutritionSummaryHttpResponse response = model.ToHttpResponse();

        Assert.Equal(4, response.LoggedCycleDays);
        Assert.Equal(3, response.DaysWithMeals);
        Assert.Equal(2, response.BleedingDays);
        Assert.Equal(2100, response.AverageCaloriesOnBleedingDays);
        Assert.Equal(28, response.AverageFiberOnNonBleedingCycleDays);
        Assert.Equal(6.5, response.AveragePainImpactOnDaysWithMeals);
        Assert.True(response.HasEnoughNutritionData);
    }

    [Fact]
    public void CycleLogDayModel_ToHttpResponse_MapsNestedEntries() {
        var cycleProfileId = Guid.NewGuid();
        var bleedingId = Guid.NewGuid();
        var symptomId = Guid.NewGuid();
        var signalId = Guid.NewGuid();
        DateTime date = new(2026, 4, 3, 0, 0, 0, DateTimeKind.Utc);
        var model = new CycleLogDayModel(
            cycleProfileId,
            date,
            [
                new BleedingEntryModel(
                    bleedingId,
                    cycleProfileId,
                    date,
                    BleedingType.Spotting,
                    CycleFlowLevel.Light,
                    PainImpact: 1,
                    Notes: "light"),
            ],
            [
                new CycleSymptomEntryModel(
                    symptomId,
                    cycleProfileId,
                    date,
                    CycleSymptomCategory.Headache,
                    Intensity: 3,
                    Tags: ["migraine"],
                    Note: "afternoon"),
            ],
            new FertilitySignalModel(
                signalId,
                cycleProfileId,
                date,
                BasalBodyTemperatureCelsius: 36.8,
                OvulationTestResult: OvulationTestResult.Positive,
                CervicalFluid: "egg-white",
                HadSex: true,
                Notes: "peak"));

        CycleLogDayHttpResponse response = model.ToHttpResponse();

        Assert.Equal(cycleProfileId, response.CycleProfileId);
        Assert.Equal(date, response.Date);
        Assert.Equal(bleedingId, Assert.Single(response.BleedingEntries).Id);
        CycleSymptomEntryHttpResponse symptom = Assert.Single(response.Symptoms);
        Assert.Equal(symptomId, symptom.Id);
        Assert.Equal(cycleProfileId, symptom.CycleProfileId);
        Assert.Equal(date, symptom.Date);
        Assert.Equal((int)CycleSymptomCategory.Headache, symptom.Category);
        Assert.Equal(3, symptom.Intensity);
        Assert.Equal("migraine", Assert.Single(symptom.Tags));
        Assert.Equal("afternoon", symptom.Note);
        Assert.Equal(signalId, response.FertilitySignal!.Id);
        Assert.Equal((int)OvulationTestResult.Positive, response.FertilitySignal.OvulationTestResult);
    }

    [Fact]
    public void CycleFactorModel_ToHttpResponse_MapsAllFields() {
        var id = Guid.NewGuid();
        var cycleProfileId = Guid.NewGuid();
        DateTime startDate = new(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        var model = new CycleFactorModel(id, cycleProfileId, CycleFactorType.Perimenopause, startDate, startDate.AddDays(10), "notes");

        CycleFactorHttpResponse response = model.ToHttpResponse();

        Assert.Equal(id, response.Id);
        Assert.Equal(cycleProfileId, response.CycleProfileId);
        Assert.Equal((int)CycleFactorType.Perimenopause, response.Type);
        Assert.Equal(startDate, response.StartDate);
        Assert.Equal(startDate.AddDays(10), response.EndDate);
        Assert.Equal("notes", response.Notes);
    }

    [Fact]
    public void FertilitySignalModel_ToHttpResponse_MapsNullOvulationTestResult() {
        var id = Guid.NewGuid();
        var cycleProfileId = Guid.NewGuid();
        DateTime date = new(2026, 4, 2, 0, 0, 0, DateTimeKind.Utc);
        var model = new FertilitySignalModel(id, cycleProfileId, date, 36.5, OvulationTestResult: null, "sticky", HadSex: false, Notes: null);

        FertilitySignalHttpResponse response = model.ToHttpResponse();

        Assert.Equal(id, response.Id);
        Assert.Equal(cycleProfileId, response.CycleProfileId);
        Assert.Equal(date, response.Date);
        Assert.Equal(36.5, response.BasalBodyTemperatureCelsius);
        Assert.Null(response.OvulationTestResult);
        Assert.Equal("sticky", response.CervicalFluid);
        Assert.False(response.HadSex);
        Assert.Null(response.Notes);
    }
}
