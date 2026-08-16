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
    public void CyclePredictionService_CalculatePredictions_WithNoCompletedCycles_ReturnsInsufficientData() {
        var profile = CycleProfile.Create(UserId.New(), new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc), showFertilityEstimates: true);

        CyclePredictionsModel predictions = CyclePredictionService.CalculatePredictions(profile, new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc));

        Assert.Null(predictions.NextPeriodStartFrom);
        Assert.Equal("Insufficient", predictions.DataSufficiency);
        Assert.Equal(0, predictions.CompletedCycleCount);
        Assert.Contains("insufficient_completed_cycles", predictions.ReasonCodes, StringComparer.Ordinal);
        Assert.Equal("period-v2.0", predictions.AlgorithmVersion);
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
        Assert.Contains("prediction_paused_by_state", predictions.ReasonCodes, StringComparer.Ordinal);
    }

    [Fact]
    public void CyclePredictionService_CalculatePredictions_WithEndedPredictionLimitingFactor_ReturnsRange() {
        var profile = CycleProfile.Create(UserId.New(), new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc), showFertilityEstimates: true);
        profile.UpsertFactor(
            CycleFactorType.HormonalContraception,
            new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc),
            notes: null);
        AddBleedingEpisode(profile, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        AddBleedingEpisode(profile, new DateTime(2026, 1, 29, 0, 0, 0, DateTimeKind.Utc));
        AddBleedingEpisode(profile, new DateTime(2026, 2, 26, 0, 0, 0, DateTimeKind.Utc));
        AddBleedingEpisode(profile, new DateTime(2026, 3, 25, 0, 0, 0, DateTimeKind.Utc));

        CyclePredictionsModel predictions = CyclePredictionService.CalculatePredictions(profile, new DateTime(2026, 3, 25, 0, 0, 0, DateTimeKind.Utc));

        Assert.NotNull(predictions.NextPeriodStartFrom);
        Assert.Null(predictions.OvulationFrom);
        Assert.Contains("fertility_estimate_not_available_in_v2", predictions.ReasonCodes, StringComparer.Ordinal);
    }

    [Fact]
    public void CyclePredictionService_CalculatePredictions_UsesEpisodeStartsAndSparseHistoryRange() {
        var profile = CycleProfile.Create(UserId.New(), new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), showFertilityEstimates: false);
        AddBleedingEpisode(profile, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        AddBleedingEpisode(profile, new DateTime(2026, 1, 29, 0, 0, 0, DateTimeKind.Utc));
        AddBleedingEpisode(profile, new DateTime(2026, 2, 26, 0, 0, 0, DateTimeKind.Utc));
        AddBleedingEpisode(profile, new DateTime(2026, 3, 25, 0, 0, 0, DateTimeKind.Utc));

        CyclePredictionsModel predictions = CyclePredictionService.CalculatePredictions(profile, new DateTime(2026, 3, 25, 0, 0, 0, DateTimeKind.Utc));

        Assert.Equal(new DateTime(2026, 4, 19, 0, 0, 0, DateTimeKind.Utc), predictions.NextPeriodStartFrom);
        Assert.Equal(new DateTime(2026, 4, 24, 0, 0, 0, DateTimeKind.Utc), predictions.NextPeriodStartTo);
        Assert.Equal(3, predictions.CompletedCycleCount);
        Assert.Equal("Limited", predictions.DataSufficiency);
        Assert.Equal("Consistent", predictions.PatternConsistency);
    }

    [Fact]
    public void CyclePredictionService_CalculatePredictions_BridgesOneUnloggedDayWithinEpisode() {
        var profile = CycleProfile.Create(UserId.New(), new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        profile.UpsertBleedingEntry(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), BleedingType.Bleeding, CycleFlowLevel.Medium, painImpact: null, notes: null);
        profile.UpsertBleedingEntry(new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc), BleedingType.Bleeding, CycleFlowLevel.Medium, painImpact: null, notes: null);
        AddBleedingEpisode(profile, new DateTime(2026, 1, 29, 0, 0, 0, DateTimeKind.Utc));
        AddBleedingEpisode(profile, new DateTime(2026, 2, 26, 0, 0, 0, DateTimeKind.Utc));
        AddBleedingEpisode(profile, new DateTime(2026, 3, 26, 0, 0, 0, DateTimeKind.Utc));

        CyclePredictionsModel predictions = CyclePredictionService.CalculatePredictions(profile, new DateTime(2026, 3, 26, 0, 0, 0, DateTimeKind.Utc));

        Assert.Equal(3, predictions.CompletedCycleCount);
        Assert.Equal(new DateTime(2026, 4, 21, 0, 0, 0, DateTimeKind.Utc), predictions.NextPeriodStartFrom);
    }

    [Fact]
    public void CyclePredictionService_CalculatePredictions_RollsExpiredRangeForward() {
        var profile = CycleProfile.Create(UserId.New(), new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        AddBleedingEpisode(profile, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        AddBleedingEpisode(profile, new DateTime(2026, 1, 29, 0, 0, 0, DateTimeKind.Utc));
        AddBleedingEpisode(profile, new DateTime(2026, 2, 26, 0, 0, 0, DateTimeKind.Utc));
        AddBleedingEpisode(profile, new DateTime(2026, 3, 26, 0, 0, 0, DateTimeKind.Utc));

        DateTime currentDate = new(2026, 8, 16, 0, 0, 0, DateTimeKind.Utc);
        CyclePredictionsModel predictions = CyclePredictionService.CalculatePredictions(profile, currentDate);

        Assert.True(predictions.NextPeriodStartTo >= currentDate);
    }

    [Fact]
    public void CyclePredictionService_ForReadModel_DoesNotUseSpottingOrLastBleedingDayAsAnchor() {
        var profileId = Guid.NewGuid();
        DateTime trackingStart = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime latestEpisodeStart = new(2026, 3, 25, 0, 0, 0, DateTimeKind.Utc);
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
                new BleedingEntryReadModel(Guid.NewGuid(), profileId, trackingStart.AddDays(-1), BleedingType.Spotting, CycleFlowLevel.Light, PainImpact: null, Notes: null),
                new BleedingEntryReadModel(Guid.NewGuid(), profileId, trackingStart, BleedingType.Bleeding, CycleFlowLevel.Medium, PainImpact: null, Notes: null),
                new BleedingEntryReadModel(Guid.NewGuid(), profileId, new DateTime(2026, 1, 29, 0, 0, 0, DateTimeKind.Utc), BleedingType.Bleeding, CycleFlowLevel.Medium, PainImpact: null, Notes: null),
                new BleedingEntryReadModel(Guid.NewGuid(), profileId, new DateTime(2026, 2, 26, 0, 0, 0, DateTimeKind.Utc), BleedingType.Bleeding, CycleFlowLevel.Medium, PainImpact: null, Notes: null),
                new BleedingEntryReadModel(Guid.NewGuid(), profileId, latestEpisodeStart, BleedingType.Bleeding, CycleFlowLevel.Medium, PainImpact: null, Notes: null),
                new BleedingEntryReadModel(Guid.NewGuid(), profileId, latestEpisodeStart.AddDays(1), BleedingType.Bleeding, CycleFlowLevel.Medium, PainImpact: null, Notes: null),
            ],
            SymptomEntries: [],
            Factors: [],
            FertilitySignals: []);

        CyclePredictionsModel predictions = CyclePredictionService.CalculatePredictions(profile, latestEpisodeStart);

        Assert.Equal(latestEpisodeStart.AddDays(25), predictions.NextPeriodStartFrom);
        Assert.Equal(latestEpisodeStart.AddDays(30), predictions.NextPeriodStartTo);
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

    private static void AddBleedingEpisode(CycleProfile profile, DateTime startDate) {
        for (int day = 0; day < 5; day++) {
            profile.UpsertBleedingEntry(
                startDate.AddDays(day),
                BleedingType.Bleeding,
                CycleFlowLevel.Medium,
                painImpact: null,
                notes: null);
        }
    }
}
