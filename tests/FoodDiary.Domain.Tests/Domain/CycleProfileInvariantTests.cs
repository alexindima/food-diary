using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using System.Reflection;

namespace FoodDiary.Domain.Tests.Domain;

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
    public void UpdateSettings_WithNotes_UpdatesNotes() {
        var profile = CycleProfile.Create(UserId.New(), DateTime.UtcNow, notes: "old");

        profile.UpdateSettings(new CycleProfileSettings(
            CycleTrackingMode.TryingToConceive,
            AverageCycleLength: null,
            AveragePeriodLength: null,
            LutealLength: null,
            IsRegular: null,
            IsOnboardingComplete: null,
            ShowFertilityEstimates: null,
            DiscreetNotifications: null,
            Notes: " updated ",
            ClearNotes: false));

        Assert.Equal("updated", profile.Notes);
        Assert.NotNull(profile.ModifiedOnUtc);
    }

    [Fact]
    public void UpdateSettings_WithUnknownMode_Throws() {
        var profile = CycleProfile.Create(UserId.New(), DateTime.UtcNow);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            profile.UpdateSettings(new CycleProfileSettings(
                (CycleTrackingMode)999,
                AverageCycleLength: null,
                AveragePeriodLength: null,
                LutealLength: null,
                IsRegular: null,
                IsOnboardingComplete: null,
                ShowFertilityEstimates: null,
                DiscreetNotifications: null,
                Notes: null,
                ClearNotes: false)));
    }

    [Fact]
    public void UpsertBleedingEntry_WithRepeatedDateAndType_ReplacesExistingEntry() {
        var profile = CycleProfile.Create(UserId.New(), DateTime.UtcNow);
        DateTime date = new(2026, 4, 2, 18, 30, 0, DateTimeKind.Unspecified);

        BleedingEntry first = profile.UpsertBleedingEntry(date, BleedingType.Bleeding, CycleFlowLevel.Light, painImpact: 2, notes: " note ");
        BleedingEntry second = profile.UpsertBleedingEntry(date, BleedingType.Bleeding, CycleFlowLevel.Heavy, painImpact: 4, notes: "updated");

        Assert.Same(first, second);
        Assert.Single(profile.BleedingEntries);
        Assert.Multiple(
            () => Assert.Equal(CycleFlowLevel.Heavy, second.Flow),
            () => Assert.Equal(4, second.PainImpact),
            () => Assert.Equal("updated", second.Notes),
            () => Assert.Equal(new DateTime(2026, 4, 2, 0, 0, 0, DateTimeKind.Utc), second.Date));
    }

    [Fact]
    public void UpsertBleedingEntry_WithExistingEntry_RecalculatesConfidenceAndSetsModified() {
        var profile = CycleProfile.Create(UserId.New(), DateTime.UtcNow);
        DateTime date = new(2026, 4, 2, 0, 0, 0, DateTimeKind.Utc);
        BleedingEntry first = profile.UpsertBleedingEntry(date, BleedingType.Bleeding, CycleFlowLevel.Light, painImpact: null, notes: null);

        BleedingEntry updated = profile.UpsertBleedingEntry(date, BleedingType.Bleeding, CycleFlowLevel.Medium, painImpact: 3, notes: "updated");

        Assert.Same(first, updated);
        Assert.Equal(CycleConfidence.Learning, profile.Confidence);
        Assert.NotNull(profile.ModifiedOnUtc);
    }

    [Fact]
    public void BleedingEntry_PrivateConstructor_CreatesMaterializationInstance() {
        ConstructorInfo constructor = typeof(BleedingEntry).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            Type.EmptyTypes,
            modifiers: null)!;

        BleedingEntry entry = Assert.IsType<BleedingEntry>(constructor.Invoke([]));

        Assert.Equal(BleedingEntryId.Empty, entry.Id);
    }

    [Fact]
    public void BleedingEntry_Create_WithEmptyCycleProfileId_Throws() {
        Assert.Throws<ArgumentException>(() =>
            BleedingEntry.Create(CycleProfileId.Empty, DateTime.UtcNow, BleedingType.Bleeding, CycleFlowLevel.Light, painImpact: null, notes: null));
    }

    [Theory]
    [InlineData(999, 1)]
    [InlineData(1, 999)]
    public void BleedingEntry_Create_WithUnknownEnumValue_Throws(int type, int flow) {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            BleedingEntry.Create(CycleProfileId.New(), DateTime.UtcNow, (BleedingType)type, (CycleFlowLevel)flow, painImpact: null, notes: null));
    }

    [Fact]
    public void BleedingEntry_Update_WithClearNotes_ClearsNotes() {
        var entry = BleedingEntry.Create(
            CycleProfileId.New(),
            DateTime.UtcNow,
            BleedingType.Bleeding,
            CycleFlowLevel.Light,
            painImpact: null,
            notes: "notes");

        entry.Update(CycleFlowLevel.Medium, painImpact: 3, notes: null, clearNotes: true);

        Assert.Multiple(
            () => Assert.Null(entry.Notes),
            () => Assert.Equal(CycleFlowLevel.Medium, entry.Flow),
            () => Assert.Equal(3, entry.PainImpact));
        Assert.NotNull(entry.ModifiedOnUtc);
    }

    [Fact]
    public void BleedingEntry_Update_WithUnknownFlow_Throws() {
        var entry = BleedingEntry.Create(
            CycleProfileId.New(),
            DateTime.UtcNow,
            BleedingType.Bleeding,
            CycleFlowLevel.Light,
            painImpact: null,
            notes: null);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            entry.Update((CycleFlowLevel)999, painImpact: null, notes: null, clearNotes: false));
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
    public void GetLastBleedingStart_WhenBleedingAndSpottingExist_ReturnsLatestBleedingDate() {
        var profile = CycleProfile.Create(UserId.New(), DateTime.UtcNow);
        DateTime firstBleeding = new(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime spotting = new(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc);
        DateTime secondBleeding = new(2026, 4, 3, 0, 0, 0, DateTimeKind.Utc);
        profile.UpsertBleedingEntry(firstBleeding, BleedingType.Bleeding, CycleFlowLevel.Light, painImpact: null, notes: null);
        profile.UpsertBleedingEntry(spotting, BleedingType.Spotting, CycleFlowLevel.Light, painImpact: null, notes: null);
        profile.UpsertBleedingEntry(secondBleeding, BleedingType.Bleeding, CycleFlowLevel.Medium, painImpact: null, notes: null);

        DateTime? lastBleedingStart = profile.GetLastBleedingStart();

        Assert.Equal(secondBleeding, lastBleedingStart);
    }

    [Fact]
    public void GetLastBleedingStart_WhenOnlySpottingExists_ReturnsNull() {
        var profile = CycleProfile.Create(UserId.New(), DateTime.UtcNow);
        profile.UpsertBleedingEntry(
            new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc),
            BleedingType.Spotting,
            CycleFlowLevel.Light,
            painImpact: null,
            notes: null);

        DateTime? lastBleedingStart = profile.GetLastBleedingStart();

        Assert.Null(lastBleedingStart);
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

        Assert.Multiple(
            () => Assert.Same(entry, updated),
            () => Assert.Equal(4, updated.Intensity),
            () => Assert.Equal(["mild"], updated.Tags),
            () => Assert.Null(updated.Note));
    }

    [Fact]
    public void CycleSymptomEntry_PrivateConstructor_CreatesMaterializationInstance() {
        ConstructorInfo constructor = typeof(CycleSymptomEntry).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            Type.EmptyTypes,
            modifiers: null)!;

        CycleSymptomEntry entry = Assert.IsType<CycleSymptomEntry>(constructor.Invoke([]));

        Assert.Equal(CycleSymptomEntryId.Empty, entry.Id);
    }

    [Fact]
    public void CycleSymptomEntry_Create_WithEmptyCycleProfileId_Throws() {
        Assert.Throws<ArgumentException>(() =>
            CycleSymptomEntry.Create(CycleProfileId.Empty, DateTime.UtcNow, CycleSymptomCategory.Bloating, 5, [], note: null));
    }

    [Fact]
    public void CycleSymptomEntry_Create_WithUnknownCategory_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CycleSymptomEntry.Create(CycleProfileId.New(), DateTime.UtcNow, (CycleSymptomCategory)999, 5, [], note: null));
    }

    [Fact]
    public void CycleSymptomEntry_Update_WithNote_UpdatesNote() {
        var entry = CycleSymptomEntry.Create(
            CycleProfileId.New(),
            DateTime.UtcNow,
            CycleSymptomCategory.Bloating,
            5,
            [],
            note: null);

        entry.Update(intensity: 6, tags: ["tag"], note: " updated ", clearNote: false);

        Assert.Equal("updated", entry.Note);
        Assert.NotNull(entry.ModifiedOnUtc);
    }

    [Fact]
    public void UpsertFactor_WithActiveHormonalContraception_LowersConfidence() {
        var profile = CycleProfile.Create(UserId.New(), DateTime.UtcNow, isRegular: true);

        profile.UpsertFactor(CycleFactorType.HormonalContraception, DateTime.UtcNow, endDate: null, notes: null);

        Assert.Equal(CycleConfidence.Low, profile.Confidence);
    }

    [Fact]
    public void UpsertFactor_WithExistingFactor_UpdatesAndReturnsExisting() {
        var profile = CycleProfile.Create(UserId.New(), DateTime.UtcNow, isRegular: true);
        DateTime startDate = new(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        CycleFactor first = profile.UpsertFactor(CycleFactorType.NonHormonalContraception, startDate, endDate: null, notes: "old");

        CycleFactor updated = profile.UpsertFactor(CycleFactorType.NonHormonalContraception, startDate, endDate: startDate.AddDays(2), notes: "updated");

        Assert.Multiple(
            () => Assert.Same(first, updated),
            () => Assert.Equal(startDate.AddDays(2), updated.EndDate),
            () => Assert.Equal("updated", updated.Notes));
        Assert.NotNull(profile.ModifiedOnUtc);
    }

    [Fact]
    public void CycleFactor_PrivateConstructor_CreatesMaterializationInstance() {
        ConstructorInfo constructor = typeof(CycleFactor).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            Type.EmptyTypes,
            modifiers: null)!;

        CycleFactor factor = Assert.IsType<CycleFactor>(constructor.Invoke([]));

        Assert.Equal(CycleFactorId.Empty, factor.Id);
    }

    [Fact]
    public void CycleFactor_Create_WithEndDateBeforeStartDate_Throws() {
        DateTime startDate = new(2026, 4, 2, 0, 0, 0, DateTimeKind.Utc);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CycleFactor.Create(CycleProfileId.New(), CycleFactorType.NonHormonalContraception, startDate, startDate.AddDays(-1), notes: null));
    }

    [Fact]
    public void CycleFactor_Create_WithEmptyCycleProfileId_Throws() {
        Assert.Throws<ArgumentException>(() =>
            CycleFactor.Create(CycleProfileId.Empty, CycleFactorType.NonHormonalContraception, DateTime.UtcNow, endDate: null, notes: null));
    }

    [Fact]
    public void CycleFactor_Create_WithUnknownType_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CycleFactor.Create(CycleProfileId.New(), (CycleFactorType)999, DateTime.UtcNow, endDate: null, notes: null));
    }

    [Fact]
    public void CycleFactor_Update_WithClearNotes_ClearsNotes() {
        var factor = CycleFactor.Create(
            CycleProfileId.New(),
            CycleFactorType.NonHormonalContraception,
            DateTime.UtcNow,
            endDate: null,
            notes: "notes");

        factor.Update(endDate: null, notes: null, clearNotes: true);

        Assert.Null(factor.Notes);
        Assert.NotNull(factor.ModifiedOnUtc);
    }

    [Fact]
    public void CycleFactor_Update_WithEndDateBeforeStartDate_Throws() {
        DateTime startDate = new(2026, 4, 2, 0, 0, 0, DateTimeKind.Utc);
        var factor = CycleFactor.Create(
            CycleProfileId.New(),
            CycleFactorType.NonHormonalContraception,
            startDate,
            endDate: null,
            notes: null);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            factor.Update(startDate.AddDays(-1), notes: null, clearNotes: false));
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

    [Fact]
    public void UpsertFertilitySignal_WithExistingSignal_UpdatesAndReturnsExisting() {
        var profile = CycleProfile.Create(UserId.New(), DateTime.UtcNow);
        DateTime date = new(2026, 4, 3, 0, 0, 0, DateTimeKind.Utc);
        FertilitySignal first = profile.UpsertFertilitySignal(
            date,
            basalBodyTemperatureCelsius: 36.4,
            ovulationTestResult: OvulationTestResult.Negative,
            cervicalFluid: null,
            hadSex: false,
            notes: "old");

        FertilitySignal updated = profile.UpsertFertilitySignal(
            date,
            basalBodyTemperatureCelsius: 36.8,
            ovulationTestResult: OvulationTestResult.Positive,
            cervicalFluid: "egg white",
            hadSex: true,
            notes: "updated");

        Assert.Multiple(
            () => Assert.Same(first, updated),
            () => Assert.Equal(36.8, updated.BasalBodyTemperatureCelsius),
            () => Assert.Equal(OvulationTestResult.Positive, updated.OvulationTestResult),
            () => Assert.Equal("egg white", updated.CervicalFluid),
            () => Assert.True(updated.HadSex),
            () => Assert.Equal("updated", updated.Notes));
        Assert.NotNull(profile.ModifiedOnUtc);
    }

    [Fact]
    public void FertilitySignal_PrivateConstructor_CreatesMaterializationInstance() {
        ConstructorInfo constructor = typeof(FertilitySignal).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            Type.EmptyTypes,
            modifiers: null)!;

        FertilitySignal signal = Assert.IsType<FertilitySignal>(constructor.Invoke([]));

        Assert.Equal(FertilitySignalId.Empty, signal.Id);
    }

    [Fact]
    public void FertilitySignal_Create_WithNullTemperature_AllowsMissingTemperature() {
        var signal = FertilitySignal.Create(
            CycleProfileId.New(),
            DateTime.UtcNow,
            basalBodyTemperatureCelsius: null,
            ovulationTestResult: null,
            cervicalFluid: null,
            hadSex: null,
            notes: null);

        Assert.Null(signal.BasalBodyTemperatureCelsius);
    }

    [Fact]
    public void FertilitySignal_Create_WithEmptyCycleProfileId_Throws() {
        Assert.Throws<ArgumentException>(() =>
            FertilitySignal.Create(
                CycleProfileId.Empty,
                DateTime.UtcNow,
                basalBodyTemperatureCelsius: null,
                ovulationTestResult: null,
                cervicalFluid: null,
                hadSex: null,
                notes: null));
    }

    [Fact]
    public void FertilitySignal_Create_WithUnknownOvulationTestResult_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            FertilitySignal.Create(
                CycleProfileId.New(),
                DateTime.UtcNow,
                basalBodyTemperatureCelsius: null,
                ovulationTestResult: (OvulationTestResult)999,
                cervicalFluid: null,
                hadSex: null,
                notes: null));
    }

    [Fact]
    public void FertilitySignal_Update_WithClearNotes_ClearsNotes() {
        var signal = FertilitySignal.Create(
            CycleProfileId.New(),
            DateTime.UtcNow,
            basalBodyTemperatureCelsius: null,
            ovulationTestResult: null,
            cervicalFluid: null,
            hadSex: null,
            notes: "notes");

        signal.Update(
            basalBodyTemperatureCelsius: null,
            ovulationTestResult: null,
            cervicalFluid: null,
            hadSex: null,
            notes: null,
            clearNotes: true);

        Assert.Null(signal.Notes);
        Assert.NotNull(signal.ModifiedOnUtc);
    }

    [Fact]
    public void FertilitySignal_Update_WithUnknownOvulationTestResult_Throws() {
        var signal = FertilitySignal.Create(
            CycleProfileId.New(),
            DateTime.UtcNow,
            basalBodyTemperatureCelsius: null,
            ovulationTestResult: null,
            cervicalFluid: null,
            hadSex: null,
            notes: null);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            signal.Update(
                basalBodyTemperatureCelsius: null,
                ovulationTestResult: (OvulationTestResult)999,
                cervicalFluid: null,
                hadSex: null,
                notes: null,
                clearNotes: false));
    }

    [Fact]
    public void ClearDay_WhenNoEntriesExist_ReturnsFalseAndDoesNotSetModified() {
        var profile = CycleProfile.Create(UserId.New(), DateTime.UtcNow);

        bool removed = profile.ClearDay(DateTime.UtcNow);

        Assert.False(removed);
        Assert.Null(profile.ModifiedOnUtc);
    }
}
