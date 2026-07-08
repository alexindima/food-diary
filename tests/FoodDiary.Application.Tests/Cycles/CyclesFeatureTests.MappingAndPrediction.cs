using FoodDiary.Application.Abstractions.Cycles.Models;
using FoodDiary.Application.Cycles.Commands.UpsertCycleDay;
using FoodDiary.Application.Cycles.Mappings;
using FoodDiary.Application.Cycles.Models;
using FoodDiary.Application.Cycles.Services;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Cycles;

public partial class CyclesFeatureTests {

    [Fact]
    public void CycleMappings_ToModel_SortsLogsByDate() {
        var profile = CycleProfile.Create(UserId.New(), DateTime.UtcNow);
        profile.UpsertBleedingEntry(new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc), BleedingType.Bleeding, CycleFlowLevel.Light, painImpact: null, notes: null);
        profile.UpsertBleedingEntry(new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), BleedingType.Bleeding, CycleFlowLevel.Heavy, painImpact: null, notes: null);
        profile.UpsertSymptomEntry(new DateTime(2026, 2, 3, 0, 0, 0, DateTimeKind.Utc), CycleSymptomCategory.Pain, 4, [], note: null);
        profile.UpsertSymptomEntry(new DateTime(2026, 2, 2, 0, 0, 0, DateTimeKind.Utc), CycleSymptomCategory.Craving, 6, [], note: null);
        profile.UpsertFertilitySignal(new DateTime(2026, 2, 5, 0, 0, 0, DateTimeKind.Utc), 36.7, OvulationTestResult.Positive, "egg white", hadSex: true, notes: null);
        profile.UpsertFertilitySignal(new DateTime(2026, 2, 4, 0, 0, 0, DateTimeKind.Utc), 36.5, OvulationTestResult.Negative, "sticky", hadSex: false, notes: null);

        CycleModel response = profile.ToModel();

        Assert.Equal(
            [new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 2, 10, 0, 0, 0, DateTimeKind.Utc)],
            response.BleedingEntries.Select(day => day.Date));
        Assert.Equal(
            [new DateTime(2026, 2, 2, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 2, 3, 0, 0, 0, DateTimeKind.Utc)],
            response.Symptoms.Select(day => day.Date));
        Assert.Equal(
            [new DateTime(2026, 2, 4, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 2, 5, 0, 0, 0, DateTimeKind.Utc)],
            response.FertilitySignals.Select(day => day.Date));
    }

    [Fact]
    public void CyclePredictionService_CalculatePredictions_ReturnsRangeAndConfidence() {
        var profile = CycleProfile.Create(UserId.New(), new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc), showFertilityEstimates: true);

        CyclePredictionsModel predictions = CyclePredictionService.CalculatePredictions(profile);

        Assert.NotNull(predictions.NextPeriodStartFrom);
        Assert.NotNull(predictions.NextPeriodStartTo);
        Assert.NotNull(predictions.OvulationFrom);
        Assert.Equal("Learning", predictions.Confidence);
    }

    [Fact]
    public void CyclePredictionService_CalculatePredictions_WithActivePredictionLimitingFactor_ReturnsLimitedPrediction() {
        var profile = CycleProfile.Create(UserId.New(), new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc), showFertilityEstimates: true);
        profile.UpsertFactor(
            CycleFactorType.HormonalContraception,
            new DateTime(2026, 4, 2, 0, 0, 0, DateTimeKind.Utc),
            endDate: null,
            notes: null);

        CyclePredictionsModel predictions = CyclePredictionService.CalculatePredictions(profile);

        Assert.Null(predictions.NextPeriodStartFrom);
        Assert.Null(predictions.NextPeriodStartTo);
        Assert.Null(predictions.OvulationFrom);
        Assert.Null(predictions.OvulationTo);
        Assert.Equal("Predictions are limited by the active tracking mode.", predictions.Rationale);
    }

    [Fact]
    public void CyclePredictionService_CalculatePredictions_WithEndedPredictionLimitingFactor_ReturnsRange() {
        var profile = CycleProfile.Create(UserId.New(), new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc), showFertilityEstimates: true);
        profile.UpsertFactor(
            CycleFactorType.HormonalContraception,
            new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc),
            notes: null);

        CyclePredictionsModel predictions = CyclePredictionService.CalculatePredictions(profile);

        Assert.NotNull(predictions.NextPeriodStartFrom);
        Assert.NotNull(predictions.OvulationFrom);
    }

    [Theory]
    [InlineData(CycleConfidence.High, 1)]
    [InlineData(CycleConfidence.Medium, 2)]
    [InlineData(CycleConfidence.Low, 4)]
    [InlineData(CycleConfidence.Learning, 7)]
    public void CyclePredictionService_CalculatePredictions_UsesConfidenceWindow(CycleConfidence confidence, int expectedWindowDays) {
        DateTime trackingStart = new(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        var profile = CycleProfile.Create(UserId.New(), trackingStart, showFertilityEstimates: true);
        SetPrivateProperty(profile, nameof(CycleProfile.Confidence), confidence);

        CyclePredictionsModel predictions = CyclePredictionService.CalculatePredictions(profile);

        DateTime expectedNextPeriodStart = trackingStart.AddDays(profile.AverageCycleLength);
        Assert.Equal(expectedNextPeriodStart.AddDays(-expectedWindowDays), predictions.NextPeriodStartFrom);
        Assert.Equal(expectedNextPeriodStart.AddDays(expectedWindowDays), predictions.NextPeriodStartTo);
    }

    [Fact]
    public void CyclePredictionService_ForReadModel_UsesLatestBleedingAsAnchor() {
        var profileId = Guid.NewGuid();
        DateTime trackingStart = new(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime latestBleeding = new(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc);
        var profile = new CycleProfileReadModel(
            profileId,
            UserId.New().Value,
            CycleTrackingMode.PeriodTracking,
            CycleConfidence.High,
            trackingStart,
            AverageCycleLength: 28,
            AveragePeriodLength: 5,
            LutealLength: 14,
            IsRegular: true,
            IsOnboardingComplete: true,
            ShowFertilityEstimates: true,
            DiscreetNotifications: false,
            Notes: null,
            BleedingEntries: [
                new BleedingEntryReadModel(Guid.NewGuid(), profileId, trackingStart.AddDays(3), BleedingType.Spotting, CycleFlowLevel.Light, PainImpact: null, Notes: null),
                new BleedingEntryReadModel(Guid.NewGuid(), profileId, latestBleeding, BleedingType.Bleeding, CycleFlowLevel.Medium, PainImpact: null, Notes: null),
            ],
            SymptomEntries: [],
            Factors: [],
            FertilitySignals: []);

        CyclePredictionsModel predictions = CyclePredictionService.CalculatePredictions(profile);

        DateTime expectedNextPeriodStart = latestBleeding.AddDays(profile.AverageCycleLength);
        Assert.Equal(expectedNextPeriodStart.AddDays(-1), predictions.NextPeriodStartFrom);
        Assert.Equal(expectedNextPeriodStart.AddDays(1), predictions.NextPeriodStartTo);
    }

    [Fact]
    public void FertilitySignalModel_ConstructsExpectedValues() {
        var id = Guid.NewGuid();
        var profileId = Guid.NewGuid();
        var date = new DateTime(2026, 4, 3, 0, 0, 0, DateTimeKind.Utc);

        var model = new FertilitySignalModel(
            id,
            profileId,
            date,
            BasalBodyTemperatureCelsius: 36.62,
            OvulationTestResult.Positive,
            CervicalFluid: "egg white",
            HadSex: true,
            Notes: "peak");

        Assert.Equal(id, model.Id);
        Assert.Equal(profileId, model.CycleProfileId);
        Assert.Equal(date, model.Date);
        Assert.Equal(36.62, model.BasalBodyTemperatureCelsius);
        Assert.Equal(OvulationTestResult.Positive, model.OvulationTestResult);
        Assert.Equal("egg white", model.CervicalFluid);
        Assert.True(model.HadSex);
        Assert.Equal("peak", model.Notes);
    }

    [Fact]
    public void FertilitySignalCommandModel_ConstructsExpectedValues() {
        var model = new FertilitySignalCommandModel(
            BasalBodyTemperatureCelsius: 36.62,
            OvulationTestResult: (int)FoodDiary.Domain.Enums.OvulationTestResult.Positive,
            CervicalFluid: "egg white",
            HadSex: true,
            Notes: "peak",
            ClearNotes: false);

        Assert.Equal(36.62, model.BasalBodyTemperatureCelsius);
        Assert.Equal((int)FoodDiary.Domain.Enums.OvulationTestResult.Positive, model.OvulationTestResult);
        Assert.Equal("egg white", model.CervicalFluid);
        Assert.True(model.HadSex);
        Assert.Equal("peak", model.Notes);
        Assert.False(model.ClearNotes);
    }
}
