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
        var profile = CycleProfile.Create(UserId.New(), DateOnly.FromDateTime(DateTime.UtcNow));
        profile.GrantConsent(CycleConsentPurpose.FertilitySignals, DateTime.UtcNow);
        profile.UpsertBleedingEntry(new DateOnly(2026, 2, 10), BleedingType.Bleeding, CycleFlowLevel.Light, painImpact: null, notes: null);
        profile.UpsertBleedingEntry(new DateOnly(2026, 2, 1), BleedingType.Bleeding, CycleFlowLevel.Heavy, painImpact: null, notes: null);
        profile.UpsertSymptomEntry(new DateOnly(2026, 2, 3), CycleSymptomCategory.Pain, 4, [], note: null);
        profile.UpsertSymptomEntry(new DateOnly(2026, 2, 2), CycleSymptomCategory.Craving, 6, [], note: null);
        profile.UpsertFertilitySignal(new DateOnly(2026, 2, 5), 36.7, OvulationTestResult.Positive, "egg white", hadSex: true, notes: null);
        profile.UpsertFertilitySignal(new DateOnly(2026, 2, 4), 36.5, OvulationTestResult.Negative, "sticky", hadSex: false, notes: null);

        CycleModel response = profile.ToModel();

        Assert.Equal(
            [new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 10)],
            response.BleedingEntries.Select(day => day.Date));
        Assert.Equal(
            [new DateOnly(2026, 2, 2), new DateOnly(2026, 2, 3)],
            response.Symptoms.Select(day => day.Date));
        Assert.Equal(
            [new DateOnly(2026, 2, 4), new DateOnly(2026, 2, 5)],
            response.FertilitySignals.Select(day => day.Date));
    }

    [Fact]
    public void CyclePredictionService_CalculatePredictions_WithNoCompletedCycles_ReturnsInsufficientData() {
        var profile = CycleProfile.Create(UserId.New(), new DateOnly(2026, 4, 1), showFertilityEstimates: true);

        CyclePredictionsModel predictions = CyclePredictionService.CalculatePredictions(profile, new DateOnly(2026, 4, 1));

        Assert.Null(predictions.NextPeriodStartFrom);
        Assert.Equal("Insufficient", predictions.DataSufficiency);
        Assert.Equal(0, predictions.CompletedCycleCount);
        Assert.Contains("insufficient_completed_cycles", predictions.ReasonCodes, StringComparer.Ordinal);
        Assert.Equal("period-v2.0", predictions.AlgorithmVersion);
    }

    [Fact]
    public void CyclePredictionService_CalculatePredictions_WithActivePredictionLimitingFactor_ReturnsLimitedPrediction() {
        var profile = CycleProfile.Create(UserId.New(), new DateOnly(2026, 4, 1), showFertilityEstimates: true);
        profile.UpsertFactor(
            CycleFactorType.HormonalContraception,
            new DateOnly(2026, 4, 2),
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
        var profile = CycleProfile.Create(UserId.New(), new DateOnly(2026, 4, 1), showFertilityEstimates: true);
        profile.UpsertFactor(
            CycleFactorType.HormonalContraception,
            new DateOnly(2026, 3, 1),
            new DateOnly(2026, 3, 10),
            notes: null);
        AddBleedingEpisode(profile, new DateOnly(2026, 1, 1));
        AddBleedingEpisode(profile, new DateOnly(2026, 1, 29));
        AddBleedingEpisode(profile, new DateOnly(2026, 2, 26));
        AddBleedingEpisode(profile, new DateOnly(2026, 3, 25));

        CyclePredictionsModel predictions = CyclePredictionService.CalculatePredictions(profile, new DateOnly(2026, 3, 25));

        Assert.NotNull(predictions.NextPeriodStartFrom);
        Assert.Null(predictions.OvulationFrom);
        Assert.Contains("fertility_estimate_not_available_in_v2", predictions.ReasonCodes, StringComparer.Ordinal);
    }

    [Fact]
    public void CyclePredictionService_CalculatePredictions_UsesEpisodeStartsAndSparseHistoryRange() {
        var profile = CycleProfile.Create(UserId.New(), new DateOnly(2026, 1, 1), showFertilityEstimates: false);
        AddBleedingEpisode(profile, new DateOnly(2026, 1, 1));
        AddBleedingEpisode(profile, new DateOnly(2026, 1, 29));
        AddBleedingEpisode(profile, new DateOnly(2026, 2, 26));
        AddBleedingEpisode(profile, new DateOnly(2026, 3, 25));

        CyclePredictionsModel predictions = CyclePredictionService.CalculatePredictions(profile, new DateOnly(2026, 3, 25));

        Assert.Equal(new DateOnly(2026, 4, 19), predictions.NextPeriodStartFrom);
        Assert.Equal(new DateOnly(2026, 4, 24), predictions.NextPeriodStartTo);
        Assert.Equal(3, predictions.CompletedCycleCount);
        Assert.Equal("Limited", predictions.DataSufficiency);
        Assert.Equal("Consistent", predictions.PatternConsistency);
        Assert.Equal(4, predictions.UsedEpisodeCount);
        Assert.Equal(0, predictions.ExcludedEpisodeCount);
    }

    [Fact]
    public void CyclePredictionService_CalculatePredictions_DoesNotReinferExcludedConfirmedEpisode() {
        var profile = CycleProfile.Create(UserId.New(), new DateOnly(2026, 1, 1));
        DateOnly[] starts = [
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 29),
            new DateOnly(2026, 2, 26),
            new DateOnly(2026, 3, 26),
        ];
        foreach (DateOnly start in starts) {
            AddBleedingEpisode(profile, start);
        }

        MenstrualEpisode excluded = profile.ConfirmPeriodStart(starts[1]);
        profile.UpdateMenstrualEpisode(excluded.Id, starts[1], starts[1].AddDays(4), excludedFromPredictions: true);

        CyclePredictionsModel predictions = CyclePredictionService.CalculatePredictions(profile, starts[^1]);

        Assert.Null(predictions.NextPeriodStartFrom);
        Assert.Equal(3, predictions.UsedEpisodeCount);
        Assert.Equal(1, predictions.ExcludedEpisodeCount);
        Assert.Equal(2, predictions.CompletedCycleCount);
        Assert.Contains("insufficient_completed_cycles", predictions.ReasonCodes, StringComparer.Ordinal);
    }

    [Fact]
    public void CyclePredictionService_CalculatePredictions_BridgesOneUnloggedDayWithinEpisode() {
        var profile = CycleProfile.Create(UserId.New(), new DateOnly(2026, 1, 1));
        profile.UpsertBleedingEntry(new DateOnly(2026, 1, 1), BleedingType.Bleeding, CycleFlowLevel.Medium, painImpact: null, notes: null);
        profile.UpsertBleedingEntry(new DateOnly(2026, 1, 3), BleedingType.Bleeding, CycleFlowLevel.Medium, painImpact: null, notes: null);
        AddBleedingEpisode(profile, new DateOnly(2026, 1, 29));
        AddBleedingEpisode(profile, new DateOnly(2026, 2, 26));
        AddBleedingEpisode(profile, new DateOnly(2026, 3, 26));

        CyclePredictionsModel predictions = CyclePredictionService.CalculatePredictions(profile, new DateOnly(2026, 3, 26));

        Assert.Equal(3, predictions.CompletedCycleCount);
        Assert.Equal(new DateOnly(2026, 4, 21), predictions.NextPeriodStartFrom);
    }

    [Fact]
    public void CyclePredictionService_CalculatePredictions_RollsExpiredRangeForward() {
        var profile = CycleProfile.Create(UserId.New(), new DateOnly(2026, 1, 1));
        AddBleedingEpisode(profile, new DateOnly(2026, 1, 1));
        AddBleedingEpisode(profile, new DateOnly(2026, 1, 29));
        AddBleedingEpisode(profile, new DateOnly(2026, 2, 26));
        AddBleedingEpisode(profile, new DateOnly(2026, 3, 26));

        DateOnly currentDate = new(2026, 8, 16);
        CyclePredictionsModel predictions = CyclePredictionService.CalculatePredictions(profile, currentDate);

        Assert.True(predictions.NextPeriodStartTo >= currentDate);
    }

    [Fact]
    public void CyclePredictionService_ForReadModel_DoesNotUseSpottingOrLastBleedingDayAsAnchor() {
        var profileId = Guid.NewGuid();
        DateOnly trackingStart = new(2026, 1, 1);
        DateOnly latestEpisodeStart = new(2026, 3, 25);
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
                new BleedingEntryReadModel(Guid.NewGuid(), profileId, new DateOnly(2026, 1, 29), BleedingType.Bleeding, CycleFlowLevel.Medium, PainImpact: null, Notes: null),
                new BleedingEntryReadModel(Guid.NewGuid(), profileId, new DateOnly(2026, 2, 26), BleedingType.Bleeding, CycleFlowLevel.Medium, PainImpact: null, Notes: null),
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
    public void CycleProfile_RevokeFertilityConsent_DeletesSignalsAndHidesFertilityEstimates() {
        var profile = CycleProfile.Create(UserId.New(), new DateOnly(2026, 1, 1));
        profile.GrantConsent(CycleConsentPurpose.FertilitySignals, DateTime.UtcNow);
        profile.UpsertFertilitySignal(
            new DateOnly(2026, 1, 14),
            36.7,
            OvulationTestResult.Positive,
            "egg white",
            hadSex: true,
            notes: "private");

        profile.RevokeConsent(CycleConsentPurpose.FertilitySignals, DateTime.UtcNow);

        Assert.False(profile.HasActiveConsent(CycleConsentPurpose.FertilitySignals));
        Assert.Empty(profile.FertilitySignals);
        Assert.False(profile.ShowFertilityEstimates);
        Assert.Empty(profile.ToModel().FertilitySignals);
    }

    [Theory]
    [InlineData(CycleReproductiveState.Pregnancy)]
    [InlineData(CycleReproductiveState.Postpartum)]
    [InlineData(CycleReproductiveState.Lactation)]
    [InlineData(CycleReproductiveState.Perimenopause)]
    [InlineData(CycleReproductiveState.NoPeriod)]
    public void CyclePredictionService_WithJournalOnlyState_SuppressesPrediction(CycleReproductiveState state) {
        var profile = CycleProfile.Create(UserId.New(), new DateOnly(2026, 1, 1), reproductiveState: state);
        AddBleedingEpisode(profile, new DateOnly(2026, 1, 1));
        AddBleedingEpisode(profile, new DateOnly(2026, 1, 29));
        AddBleedingEpisode(profile, new DateOnly(2026, 2, 26));
        AddBleedingEpisode(profile, new DateOnly(2026, 3, 26));

        CyclePredictionsModel predictions = CyclePredictionService.CalculatePredictions(profile, new DateOnly(2026, 3, 26));

        Assert.Null(predictions.NextPeriodStartFrom);
        Assert.Contains("prediction_paused_by_state", predictions.ReasonCodes, StringComparer.Ordinal);
    }

    [Fact]
    public void CyclePredictionService_WithHistoricalCycles_CalibratesAndPersistsRevision() {
        var profile = CycleProfile.Create(UserId.New(), new DateOnly(2026, 1, 1));
        foreach (int offset in (int[])[0, 28, 57, 85, 115, 143]) {
            AddBleedingEpisode(profile, new DateOnly(2026, 1, 1).AddDays(offset));
        }

        CyclePredictionsModel predictions = CyclePredictionService.CalculatePredictions(profile, new DateOnly(2026, 5, 24));
        CyclePredictionRevisionService.Record(profile, predictions);

        Assert.Equal(2, predictions.CalibrationSampleCount);
        Assert.NotNull(predictions.HistoricalCoveragePercent);
        Assert.NotNull(predictions.MeanAbsoluteErrorDays);
        Assert.Equal("period-v2.0", Assert.Single(profile.PredictionRevisions).AlgorithmVersion);
    }

    [Fact]
    public void CycleProfile_UpdateSettings_SeparatesGoalStateAndDashboardPrivacy() {
        var profile = CycleProfile.Create(UserId.New(), new DateOnly(2026, 1, 1));

        profile.UpdateSettings(new CycleProfileSettings(
            CycleTrackingMode.PeriodTracking,
            AverageCycleLength: null,
            AveragePeriodLength: null,
            LutealLength: null,
            IsRegular: null,
            IsOnboardingComplete: null,
            ShowFertilityEstimates: null,
            DiscreetNotifications: null,
            Notes: null,
            Goal: CycleTrackingGoal.SymptomAwareness,
            ReproductiveState: CycleReproductiveState.Perimenopause,
            HideFromDashboard: true));

        Assert.Equal(CycleTrackingGoal.SymptomAwareness, profile.Goal);
        Assert.Equal(CycleReproductiveState.Perimenopause, profile.ReproductiveState);
        Assert.True(profile.HideFromDashboard);
    }

    [Fact]
    public void FertilitySignalModel_ConstructsExpectedValues() {
        var id = Guid.NewGuid();
        var profileId = Guid.NewGuid();
        var date = new DateOnly(2026, 4, 3);

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

    private static void AddBleedingEpisode(CycleProfile profile, DateOnly startDate) {
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
