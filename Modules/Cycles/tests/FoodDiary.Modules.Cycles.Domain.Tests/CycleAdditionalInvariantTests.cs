using System.Reflection;
using FoodDiary.Modules.Cycles.Domain.Entities;
using FoodDiary.Modules.Cycles.Domain.Contracts.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Cycles.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class CycleAdditionalInvariantTests {
    private static readonly DateTime Now = new(2026, 4, 28, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void CycleProfile_ConfirmPeriodStart_RejectsOverlappingConfirmedRange() {
        DateOnly start = new(2026, 4, 1);
        var profile = CycleProfile.Create(UserId.New(), start);
        MenstrualEpisode first = profile.ConfirmPeriodStart(start);
        profile.UpdateMenstrualEpisode(first.Id, start, start.AddDays(5));

        Assert.Throws<ArgumentException>(() => profile.ConfirmPeriodStart(start.AddDays(3)));
        Assert.Single(profile.MenstrualEpisodes);
    }

    [Fact]
    public void CycleProfile_Reconciliation_DoesNotInferRangeOverlappingConfirmedEpisodeEnd() {
        DateOnly start = new(2026, 4, 1);
        var profile = CycleProfile.Create(UserId.New(), start);
        MenstrualEpisode confirmed = profile.ConfirmPeriodStart(start);
        profile.UpdateMenstrualEpisode(confirmed.Id, start, start.AddDays(10));

        profile.UpsertBleedingEntry(
            start.AddDays(8),
            BleedingType.Bleeding,
            CycleFlowLevel.Medium,
            painImpact: null,
            notes: null);

        MenstrualEpisode episode = Assert.Single(profile.MenstrualEpisodes);
        Assert.Equal(MenstrualEpisodeStatus.Confirmed, episode.Status);
    }

    [Fact]
    public void CycleProfile_RecordPredictionRevision_NormalizesValidValues() {
        var profile = CycleProfile.Create(UserId.New(), new DateOnly(2026, 4, 1));

        profile.RecordPredictionRevision(
            Now,
            new DateOnly(2026, 5, 1),
            new DateOnly(2026, 5, 3),
            " high ",
            " sufficient ",
            " stable ",
            completedCycleCount: 4,
            calibrationSampleCount: 3,
            historicalCoveragePercent: 95.5,
            meanAbsoluteErrorDays: 1.2,
            reasonCodes: [" regular ", "enough-data"],
            algorithmVersion: " v2 ");

        CyclePredictionRevision revision = Assert.Single(profile.PredictionRevisions);
        Assert.Multiple(
            () => Assert.Equal("high", revision.Confidence),
            () => Assert.Equal("regular|enough-data", revision.ReasonCodes),
            () => Assert.Equal("v2", revision.AlgorithmVersion));
    }

    [Theory]
    [InlineData(-1, 0, 50, 1)]
    [InlineData(0, -1, 50, 1)]
    [InlineData(0, 0, -1, 1)]
    [InlineData(0, 0, 101, 1)]
    [InlineData(0, 0, 50, -1)]
    public void CycleProfile_RecordPredictionRevision_RejectsInvalidMetrics(
        int completedCycles,
        int samples,
        double coverage,
        double errorDays) {
        var profile = CycleProfile.Create(UserId.New(), new DateOnly(2026, 4, 1));

        Assert.Throws<ArgumentOutOfRangeException>(() => profile.RecordPredictionRevision(
            Now,
            nextPeriodStartFrom: null,
            nextPeriodStartTo: null,
            "high",
            "sufficient",
            "stable",
            completedCycles,
            samples,
            coverage,
            errorDays,
            [],
            "v2"));
        Assert.Empty(profile.PredictionRevisions);
    }

    [Fact]
    public void CycleProfile_ConfidenceAndClearDay_CoverRemainingPaths() {
        var profile = CycleProfile.Create(
            UserId.New(),
            DateOnly.FromDateTime(DateTime.UtcNow),
            mode: CycleTrackingMode.TryingToConceive,
            averageCycleLength: null,
            averagePeriodLength: null,
            lutealLength: null,
            isRegular: true,
            notes: " notes ");
        profile.UpdateSettings(new CycleProfileSettings(
            CycleTrackingMode.PeriodTracking,
            AverageCycleLength: null,
            AveragePeriodLength: null,
            LutealLength: null,
            IsRegular: true,
            IsOnboardingComplete: true,
            ShowFertilityEstimates: true,
            DiscreetNotifications: false,
            Notes: " updated ",
            ClearNotes: false));
        for (int day = 0; day < 9; day++) {
            profile.UpsertBleedingEntry(
                DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-day),
                BleedingType.Bleeding,
                CycleFlowLevel.Medium,
                painImpact: null,
                notes: null);
        }
        profile.UpsertSymptomEntry(
            DateOnly.FromDateTime(DateTime.UtcNow),
            CycleSymptomCategory.Mood,
            intensity: 5,
            tags: ["calm"],
            note: null);
        profile.GrantConsent(CycleConsentPurpose.FertilitySignals, DateTime.UtcNow);
        profile.UpsertFertilitySignal(
            DateOnly.FromDateTime(DateTime.UtcNow),
            basalBodyTemperatureCelsius: null,
            ovulationTestResult: null,
            cervicalFluid: null,
            hadSex: null,
            notes: null);
        bool cleared = profile.ClearDay(DateOnly.FromDateTime(DateTime.UtcNow));
        ReadPublicProperties(profile);

        Assert.Multiple(
            () => Assert.True(cleared),
            () => Assert.Equal(CycleConfidence.Medium, profile.Confidence),
            () => Assert.Equal("updated", profile.Notes));
    }

    [Fact]
    public void PersistenceConstructor_InitializesReadableProperties() {
        CycleProfile profile = Assert.IsType<CycleProfile>(Activator.CreateInstance(typeof(CycleProfile), nonPublic: true));
        ReadPublicProperties(profile);
    }

    private static void ReadPublicProperties(object instance) {
        foreach (PropertyInfo property in instance.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)) {
            if (property.GetIndexParameters().Length == 0) {
                property.GetValue(instance);
            }
        }
    }
}
