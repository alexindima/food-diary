using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Domain;

[ExcludeFromCodeCoverage]
public class CycleProfileInvariantTests {
    [Fact]
    public void Create_WithEmptyUserId_Throws() {
        Assert.Throws<ArgumentException>(() => CycleProfile.Create(UserId.Empty, DateTime.UtcNow));
    }

    [Fact]
    public void Create_WithUnspecifiedDate_TreatsItAsUtcDateOnly() {
        var profile = CycleProfile.Create(UserId.New(), new DateTime(2026, 3, 27, 18, 45, 0, DateTimeKind.Unspecified));

        Assert.Equal(new DateTime(2026, 3, 27, 0, 0, 0, DateTimeKind.Utc), profile.TrackingStartDate);
    }

    [Theory]
    [InlineData(17, 5, 14)]
    [InlineData(61, 5, 14)]
    [InlineData(28, 0, 14)]
    [InlineData(28, 15, 14)]
    [InlineData(28, 5, 7)]
    [InlineData(28, 5, 19)]
    public void Create_WithInvalidLengths_Throws(int averageCycleLength, int averagePeriodLength, int lutealLength) {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CycleProfile.Create(
                UserId.New(),
                DateTime.UtcNow,
                averageCycleLength: averageCycleLength,
                averagePeriodLength: averagePeriodLength,
                lutealLength: lutealLength));
    }

    [Fact]
    public void UpdateSettings_WithClearNotes_ClearsNotes() {
        var profile = CycleProfile.Create(UserId.New(), DateTime.UtcNow, notes: "notes");

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
            ClearNotes: true));

        Assert.Null(profile.Notes);
        Assert.NotNull(profile.ModifiedOnUtc);
    }

    [Fact]
    public void UpdateSettings_WithClearNotesAndValue_Throws() {
        var profile = CycleProfile.Create(UserId.New(), DateTime.UtcNow, notes: "notes");

        Assert.Throws<ArgumentException>(() =>
            profile.UpdateSettings(new CycleProfileSettings(
                CycleTrackingMode.PeriodTracking,
                AverageCycleLength: null,
                AveragePeriodLength: null,
                LutealLength: null,
                IsRegular: null,
                IsOnboardingComplete: null,
                ShowFertilityEstimates: null,
                DiscreetNotifications: null,
                Notes: "next",
                ClearNotes: true)));
    }

    [Fact]
    public void UpsertBleedingEntry_WithRepeatedDateAndType_ReplacesExistingEntry() {
        var profile = CycleProfile.Create(UserId.New(), DateTime.UtcNow);
        DateTime date = new(2026, 4, 2, 18, 30, 0, DateTimeKind.Unspecified);

        BleedingEntry first = profile.UpsertBleedingEntry(date, BleedingType.Bleeding, CycleFlowLevel.Light, painImpact: 2, notes: " note ");
        BleedingEntry second = profile.UpsertBleedingEntry(date, BleedingType.Bleeding, CycleFlowLevel.Heavy, painImpact: 4, notes: "updated");

        Assert.Same(first, second);
        Assert.Single(profile.BleedingEntries);
        Assert.Equal(CycleFlowLevel.Heavy, second.Flow);
        Assert.Equal(4, second.PainImpact);
        Assert.Equal("updated", second.Notes);
        Assert.Equal(new DateTime(2026, 4, 2, 0, 0, 0, DateTimeKind.Utc), second.Date);
    }

    [Fact]
    public void UpsertBleedingEntry_WithEnoughRegularHistory_RaisesConfidence() {
        var profile = CycleProfile.Create(UserId.New(), DateTime.UtcNow, isRegular: true);
        DateTime start = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        for (int i = 0; i < 9; i++) {
            profile.UpsertBleedingEntry(start.AddDays(i * 28), BleedingType.Bleeding, CycleFlowLevel.Medium, painImpact: null, notes: null);
        }

        Assert.Equal(CycleConfidence.High, profile.Confidence);
    }

    [Fact]
    public void UpsertSymptomEntry_NormalizesTagsAndReplacesSameCategory() {
        var profile = CycleProfile.Create(UserId.New(), DateTime.UtcNow);

        CycleSymptomEntry entry = profile.UpsertSymptomEntry(
            DateTime.UtcNow,
            CycleSymptomCategory.Bloating,
            6,
            ["  bloating ", "BLOATING", "cramp"],
            " note ");
        CycleSymptomEntry updated = profile.UpsertSymptomEntry(
            DateTime.UtcNow,
            CycleSymptomCategory.Bloating,
            intensity: 4,
            tags: ["mild"],
            note: null,
            clearNote: true);

        Assert.Same(entry, updated);
        Assert.Equal(4, updated.Intensity);
        Assert.Equal(["mild"], updated.Tags);
        Assert.Null(updated.Note);
    }

    [Fact]
    public void UpsertFactor_WithActiveHormonalContraception_LowersConfidence() {
        var profile = CycleProfile.Create(UserId.New(), DateTime.UtcNow, isRegular: true);

        profile.UpsertFactor(CycleFactorType.HormonalContraception, DateTime.UtcNow, endDate: null, notes: null);

        Assert.Equal(CycleConfidence.Low, profile.Confidence);
    }

    [Fact]
    public void UpsertFertilitySignal_ValidatesTemperature() {
        var profile = CycleProfile.Create(UserId.New(), DateTime.UtcNow);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            profile.UpsertFertilitySignal(
                DateTime.UtcNow,
                basalBodyTemperatureCelsius: 50,
                ovulationTestResult: OvulationTestResult.Positive,
                cervicalFluid: null,
                hadSex: null,
                notes: null));
    }
}
