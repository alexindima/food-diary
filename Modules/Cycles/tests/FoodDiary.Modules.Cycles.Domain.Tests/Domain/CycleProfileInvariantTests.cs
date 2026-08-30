using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using System.Reflection;

namespace FoodDiary.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public class CycleProfileInvariantTests {
    [Fact]
    public void Create_WithEmptyUserId_Throws() {
        Assert.Throws<ArgumentException>(() => CycleProfile.Create(UserId.Empty, DateOnly.FromDateTime(DateTime.UtcNow)));
    }

    [Fact]
    public void Create_WithCalendarDate_PreservesDate() {
        var profile = CycleProfile.Create(UserId.New(), new DateOnly(2026, 3, 27));

        Assert.Equal(new DateOnly(2026, 3, 27), profile.TrackingStartDate);
    }

    [Theory]
    [InlineData(CycleTrackingMode.PeriodTracking, CycleReproductiveState.Cycling)]
    [InlineData(CycleTrackingMode.TryingToConceive, CycleReproductiveState.Cycling)]
    [InlineData(CycleTrackingMode.Pregnancy, CycleReproductiveState.Pregnancy)]
    [InlineData(CycleTrackingMode.PostpartumLactation, CycleReproductiveState.Postpartum)]
    [InlineData(CycleTrackingMode.Perimenopause, CycleReproductiveState.Perimenopause)]
    [InlineData(CycleTrackingMode.NoPeriod, CycleReproductiveState.NoPeriod)]
    public void Create_WithLegacyMode_MapsReproductiveState(
        CycleTrackingMode mode,
        CycleReproductiveState expectedState) {
        var profile = CycleProfile.Create(UserId.New(), new DateOnly(2026, 3, 27), mode);

        Assert.Equal(expectedState, profile.ReproductiveState);
    }

    [Theory]
    [InlineData(CycleReproductiveState.Cycling, CycleTrackingGoal.PeriodAwareness, CycleTrackingMode.PeriodTracking)]
    [InlineData(CycleReproductiveState.Cycling, CycleTrackingGoal.TryingToConceive, CycleTrackingMode.TryingToConceive)]
    [InlineData(CycleReproductiveState.Pregnancy, CycleTrackingGoal.PeriodAwareness, CycleTrackingMode.Pregnancy)]
    [InlineData(CycleReproductiveState.Postpartum, CycleTrackingGoal.PeriodAwareness, CycleTrackingMode.PostpartumLactation)]
    [InlineData(CycleReproductiveState.Lactation, CycleTrackingGoal.PeriodAwareness, CycleTrackingMode.PostpartumLactation)]
    [InlineData(CycleReproductiveState.Perimenopause, CycleTrackingGoal.PeriodAwareness, CycleTrackingMode.Perimenopause)]
    [InlineData(CycleReproductiveState.NoPeriod, CycleTrackingGoal.PeriodAwareness, CycleTrackingMode.NoPeriod)]
    public void Create_WithExplicitGoalAndState_MapsTrackingMode(
        CycleReproductiveState reproductiveState,
        CycleTrackingGoal goal,
        CycleTrackingMode expectedMode) {
        var profile = CycleProfile.Create(
            UserId.New(),
            new DateOnly(2026, 3, 27),
            goal: goal,
            reproductiveState: reproductiveState);

        Assert.Equal(expectedMode, profile.Mode);
    }

    [Fact]
    public void ReconcileMenstrualEpisodes_IsNotExposedAsPublicMutation() {
        MethodInfo? method = typeof(CycleProfile).GetMethod(
            "ReconcileMenstrualEpisodes",
            BindingFlags.Instance | BindingFlags.Public);

        Assert.Null(method);
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
                DateOnly.FromDateTime(DateTime.UtcNow),
                averageCycleLength: averageCycleLength,
                averagePeriodLength: averagePeriodLength,
                lutealLength: lutealLength));
    }

    [Fact]
    public void UpdateSettings_WithClearNotes_ClearsNotes() {
        var profile = CycleProfile.Create(UserId.New(), DateOnly.FromDateTime(DateTime.UtcNow), notes: "notes");

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
        var profile = CycleProfile.Create(UserId.New(), DateOnly.FromDateTime(DateTime.UtcNow), notes: "notes");

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
        var profile = CycleProfile.Create(UserId.New(), DateOnly.FromDateTime(DateTime.UtcNow), notes: "old");

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
        var profile = CycleProfile.Create(UserId.New(), DateOnly.FromDateTime(DateTime.UtcNow));

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
        var profile = CycleProfile.Create(UserId.New(), DateOnly.FromDateTime(DateTime.UtcNow));
        DateOnly date = new(2026, 4, 2);

        BleedingEntry first = profile.UpsertBleedingEntry(date, BleedingType.Bleeding, CycleFlowLevel.Light, painImpact: 2, notes: " note ");
        BleedingEntry second = profile.UpsertBleedingEntry(date, BleedingType.Bleeding, CycleFlowLevel.Heavy, painImpact: 4, notes: "updated");

        Assert.Same(first, second);
        Assert.Single(profile.BleedingEntries);
        Assert.Multiple(
            () => Assert.Equal(CycleFlowLevel.Heavy, second.Flow),
            () => Assert.Equal(4, second.PainImpact),
            () => Assert.Equal("updated", second.Notes),
            () => Assert.Equal(new DateOnly(2026, 4, 2), second.Date));
    }

    [Fact]
    public void UpsertBleedingEntry_WithExistingEntry_RecalculatesConfidenceAndSetsModified() {
        var profile = CycleProfile.Create(UserId.New(), DateOnly.FromDateTime(DateTime.UtcNow));
        DateOnly date = new(2026, 4, 2);
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
            BleedingEntry.Create(CycleProfileId.Empty, DateOnly.FromDateTime(DateTime.UtcNow), BleedingType.Bleeding, CycleFlowLevel.Light, painImpact: null, notes: null));
    }

    [Theory]
    [InlineData(999, 1)]
    [InlineData(1, 999)]
    public void BleedingEntry_Create_WithUnknownEnumValue_Throws(int type, int flow) {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            BleedingEntry.Create(CycleProfileId.New(), DateOnly.FromDateTime(DateTime.UtcNow), (BleedingType)type, (CycleFlowLevel)flow, painImpact: null, notes: null));
    }

    [Fact]
    public void BleedingEntry_Update_WithClearNotes_ClearsNotes() {
        var entry = BleedingEntry.Create(
            CycleProfileId.New(),
            DateOnly.FromDateTime(DateTime.UtcNow),
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
            DateOnly.FromDateTime(DateTime.UtcNow),
            BleedingType.Bleeding,
            CycleFlowLevel.Light,
            painImpact: null,
            notes: null);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            entry.Update((CycleFlowLevel)999, painImpact: null, notes: null, clearNotes: false));
    }

    [Fact]
    public void UpsertBleedingEntry_WithEnoughRegularHistory_RaisesConfidence() {
        var profile = CycleProfile.Create(UserId.New(), DateOnly.FromDateTime(DateTime.UtcNow), isRegular: true);
        DateOnly start = new(2026, 1, 1);

        for (int i = 0; i < 9; i++) {
            profile.UpsertBleedingEntry(start.AddDays(i * 28), BleedingType.Bleeding, CycleFlowLevel.Medium, painImpact: null, notes: null);
        }

        Assert.Equal(CycleConfidence.High, profile.Confidence);
    }

    [Fact]
    public void GetLastBleedingStart_WhenBleedingAndSpottingExist_ReturnsLatestBleedingDate() {
        var profile = CycleProfile.Create(UserId.New(), DateOnly.FromDateTime(DateTime.UtcNow));
        DateOnly firstBleeding = new(2026, 4, 1);
        DateOnly spotting = new(2026, 4, 5);
        DateOnly secondBleeding = new(2026, 4, 3);
        profile.UpsertBleedingEntry(firstBleeding, BleedingType.Bleeding, CycleFlowLevel.Light, painImpact: null, notes: null);
        profile.UpsertBleedingEntry(spotting, BleedingType.Spotting, CycleFlowLevel.Light, painImpact: null, notes: null);
        profile.UpsertBleedingEntry(secondBleeding, BleedingType.Bleeding, CycleFlowLevel.Medium, painImpact: null, notes: null);

        DateOnly? lastBleedingStart = profile.GetLastBleedingStart();

        Assert.Equal(secondBleeding, lastBleedingStart);
    }

    [Fact]
    public void GetLastBleedingStart_WhenOnlySpottingExists_ReturnsNull() {
        var profile = CycleProfile.Create(UserId.New(), DateOnly.FromDateTime(DateTime.UtcNow));
        profile.UpsertBleedingEntry(
            new DateOnly(2026, 4, 5),
            BleedingType.Spotting,
            CycleFlowLevel.Light,
            painImpact: null,
            notes: null);

        DateOnly? lastBleedingStart = profile.GetLastBleedingStart();

        Assert.Null(lastBleedingStart);
    }

    [Fact]
    public void UpsertSymptomEntry_NormalizesTagsAndReplacesSameCategory() {
        var profile = CycleProfile.Create(UserId.New(), DateOnly.FromDateTime(DateTime.UtcNow));

        CycleSymptomEntry entry = profile.UpsertSymptomEntry(
            DateOnly.FromDateTime(DateTime.UtcNow),
            CycleSymptomCategory.Bloating,
            6,
            ["  bloating ", "BLOATING", "cramp"],
            " note ");
        CycleSymptomEntry updated = profile.UpsertSymptomEntry(
            DateOnly.FromDateTime(DateTime.UtcNow),
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
            CycleSymptomEntry.Create(CycleProfileId.Empty, DateOnly.FromDateTime(DateTime.UtcNow), CycleSymptomCategory.Bloating, 5, [], note: null));
    }

    [Fact]
    public void CycleSymptomEntry_Create_WithUnknownCategory_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CycleSymptomEntry.Create(CycleProfileId.New(), DateOnly.FromDateTime(DateTime.UtcNow), (CycleSymptomCategory)999, 5, [], note: null));
    }

    [Fact]
    public void CycleSymptomEntry_Create_WithNullTag_ThrowsArgumentException() {
        Assert.Throws<ArgumentException>(() => CycleSymptomEntry.Create(
            CycleProfileId.New(),
            DateOnly.FromDateTime(DateTime.UtcNow),
            CycleSymptomCategory.Bloating,
            5,
            [null!],
            note: null));
    }

    [Fact]
    public void CycleSymptomEntry_Create_WithTooManyTags_Throws() {
        string[] tags = [.. Enumerable.Repeat("tag", CycleSymptomEntry.MaxTagsCount + 1)];

        Assert.Throws<ArgumentOutOfRangeException>(() => CycleSymptomEntry.Create(
            CycleProfileId.New(),
            DateOnly.FromDateTime(DateTime.UtcNow),
            CycleSymptomCategory.Bloating,
            5,
            tags,
            note: null));
    }

    [Fact]
    public void CycleSymptomEntry_Create_WithOversizedTag_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() => CycleSymptomEntry.Create(
            CycleProfileId.New(),
            DateOnly.FromDateTime(DateTime.UtcNow),
            CycleSymptomCategory.Bloating,
            5,
            [new string('t', CycleSymptomEntry.MaxTagLength + 1)],
            note: null));
    }

    [Fact]
    public void CycleSymptomEntry_Update_WithNote_UpdatesNote() {
        var entry = CycleSymptomEntry.Create(
            CycleProfileId.New(),
            DateOnly.FromDateTime(DateTime.UtcNow),
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
        var profile = CycleProfile.Create(UserId.New(), DateOnly.FromDateTime(DateTime.UtcNow), isRegular: true);

        profile.UpsertFactor(CycleFactorType.HormonalContraception, DateOnly.FromDateTime(DateTime.UtcNow), endDate: null, notes: null);

        Assert.Equal(CycleConfidence.Low, profile.Confidence);
    }

    [Fact]
    public void UpsertFactor_WithExistingFactor_UpdatesAndReturnsExisting() {
        var profile = CycleProfile.Create(UserId.New(), DateOnly.FromDateTime(DateTime.UtcNow), isRegular: true);
        DateOnly startDate = new(2026, 4, 1);
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
        DateOnly startDate = new(2026, 4, 2);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CycleFactor.Create(CycleProfileId.New(), CycleFactorType.NonHormonalContraception, startDate, startDate.AddDays(-1), notes: null));
    }

    [Fact]
    public void CycleFactor_Create_WithEmptyCycleProfileId_Throws() {
        Assert.Throws<ArgumentException>(() =>
            CycleFactor.Create(CycleProfileId.Empty, CycleFactorType.NonHormonalContraception, DateOnly.FromDateTime(DateTime.UtcNow), endDate: null, notes: null));
    }

    [Fact]
    public void CycleFactor_Create_WithUnknownType_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CycleFactor.Create(CycleProfileId.New(), (CycleFactorType)999, DateOnly.FromDateTime(DateTime.UtcNow), endDate: null, notes: null));
    }

    [Fact]
    public void CycleFactor_Update_WithClearNotes_ClearsNotes() {
        var factor = CycleFactor.Create(
            CycleProfileId.New(),
            CycleFactorType.NonHormonalContraception,
            DateOnly.FromDateTime(DateTime.UtcNow),
            endDate: null,
            notes: "notes");

        factor.Update(endDate: null, notes: null, clearNotes: true);

        Assert.Null(factor.Notes);
        Assert.NotNull(factor.ModifiedOnUtc);
    }

    [Fact]
    public void CycleFactor_Update_WithEndDateBeforeStartDate_Throws() {
        DateOnly startDate = new(2026, 4, 2);
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
    public void GrantConsent_WhenAlreadyActive_PreservesOriginalGrantTimestamp() {
        var profile = CycleProfile.Create(UserId.New(), DateOnly.FromDateTime(DateTime.UtcNow));
        DateTime originalGrantedAtUtc = DateTime.UtcNow.AddMinutes(-5);
        profile.GrantConsent(CycleConsentPurpose.NutritionInsights, originalGrantedAtUtc);
        CycleConsent consent = Assert.Single(profile.Consents, item => item.Purpose == CycleConsentPurpose.NutritionInsights);
        DateTime? modifiedAfterInitialGrant = profile.ModifiedOnUtc;

        profile.GrantConsent(CycleConsentPurpose.NutritionInsights, originalGrantedAtUtc.AddMinutes(1));

        Assert.Multiple(
            () => Assert.Equal(originalGrantedAtUtc, consent.GrantedAtUtc),
            () => Assert.Equal(modifiedAfterInitialGrant, profile.ModifiedOnUtc),
            () => Assert.True(consent.IsActive));
    }

    [Fact]
    public void GrantConsent_WhenRegrantPredatesRevocation_ThrowsWithoutChangingConsent() {
        var profile = CycleProfile.Create(UserId.New(), DateOnly.FromDateTime(DateTime.UtcNow));
        DateTime grantedAtUtc = DateTime.UtcNow.AddMinutes(-10);
        DateTime revokedAtUtc = grantedAtUtc.AddMinutes(5);
        profile.GrantConsent(CycleConsentPurpose.NutritionInsights, grantedAtUtc);
        profile.RevokeConsent(CycleConsentPurpose.NutritionInsights, revokedAtUtc);
        CycleConsent consent = Assert.Single(profile.Consents, item => item.Purpose == CycleConsentPurpose.NutritionInsights);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            profile.GrantConsent(CycleConsentPurpose.NutritionInsights, revokedAtUtc.AddTicks(-1)));

        Assert.Multiple(
            () => Assert.Equal(grantedAtUtc, consent.GrantedAtUtc),
            () => Assert.Equal(revokedAtUtc, consent.RevokedAtUtc),
            () => Assert.False(consent.IsActive));
    }

    [Fact]
    public void ConsentTransitions_WithUnspecifiedTimestamp_ThrowWithoutChangingState() {
        var profile = CycleProfile.Create(UserId.New(), new DateOnly(2026, 4, 1));
        DateTime grantedAtUtc = new(2026, 4, 1, 12, 0, 0, DateTimeKind.Utc);
        var unspecified = DateTime.SpecifyKind(grantedAtUtc.AddMinutes(1), DateTimeKind.Unspecified);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            profile.GrantConsent(CycleConsentPurpose.NutritionInsights, unspecified));
        Assert.DoesNotContain(profile.Consents, item => item.Purpose == CycleConsentPurpose.NutritionInsights);

        profile.GrantConsent(CycleConsentPurpose.NutritionInsights, grantedAtUtc);
        CycleConsent consent = Assert.Single(
            profile.Consents,
            item => item.Purpose == CycleConsentPurpose.NutritionInsights);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            profile.RevokeConsent(CycleConsentPurpose.NutritionInsights, unspecified));
        Assert.True(consent.IsActive);
        Assert.Null(consent.RevokedAtUtc);
    }

    [Fact]
    public void UpsertFertilitySignal_ValidatesTemperature() {
        var profile = CycleProfile.Create(UserId.New(), DateOnly.FromDateTime(DateTime.UtcNow));
        profile.GrantConsent(CycleConsentPurpose.FertilitySignals, DateTime.UnixEpoch);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            profile.UpsertFertilitySignal(
                DateOnly.FromDateTime(DateTime.UtcNow),
                basalBodyTemperatureCelsius: 50,
                ovulationTestResult: OvulationTestResult.Positive,
                cervicalFluid: null,
                hadSex: null,
                notes: null));
    }

    [Fact]
    public void UpsertFertilitySignal_WithExistingSignal_UpdatesAndReturnsExisting() {
        var profile = CycleProfile.Create(UserId.New(), DateOnly.FromDateTime(DateTime.UtcNow));
        profile.GrantConsent(CycleConsentPurpose.FertilitySignals, DateTime.UnixEpoch);
        DateOnly date = new(2026, 4, 3);
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
            DateOnly.FromDateTime(DateTime.UtcNow),
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
                DateOnly.FromDateTime(DateTime.UtcNow),
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
                DateOnly.FromDateTime(DateTime.UtcNow),
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
            DateOnly.FromDateTime(DateTime.UtcNow),
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
            DateOnly.FromDateTime(DateTime.UtcNow),
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
        var profile = CycleProfile.Create(UserId.New(), DateOnly.FromDateTime(DateTime.UtcNow));

        bool removed = profile.ClearDay(DateOnly.FromDateTime(DateTime.UtcNow));

        Assert.False(removed);
        Assert.Null(profile.ModifiedOnUtc);
    }

    [Fact]
    public void ClearBleedingEntries_RemovesOnlyBleedingDataForDate() {
        DateOnly date = new(2026, 4, 2);
        var profile = CycleProfile.Create(UserId.New(), date.AddDays(-1));
        profile.GrantConsent(CycleConsentPurpose.FertilitySignals, DateTime.UnixEpoch);
        profile.UpsertBleedingEntry(date, BleedingType.Bleeding, CycleFlowLevel.Medium, painImpact: 3, notes: null);
        profile.UpsertSymptomEntry(date, CycleSymptomCategory.Pain, 4, tags: [], note: null);
        profile.UpsertFertilitySignal(
            date,
            basalBodyTemperatureCelsius: 36.6,
            ovulationTestResult: OvulationTestResult.Negative,
            cervicalFluid: null,
            hadSex: null,
            notes: null);

        bool removed = profile.ClearBleedingEntries(date);

        Assert.True(removed);
        Assert.Empty(profile.BleedingEntries);
        Assert.Single(profile.SymptomEntries);
        Assert.Single(profile.FertilitySignals);
    }

    [Fact]
    public void ClearSymptomEntries_RemovesOnlySelectedCategoriesForDate() {
        DateOnly date = new(2026, 4, 2);
        var profile = CycleProfile.Create(UserId.New(), date.AddDays(-1));
        profile.UpsertBleedingEntry(date, BleedingType.Bleeding, CycleFlowLevel.Medium, painImpact: 3, notes: null);
        profile.UpsertSymptomEntry(date, CycleSymptomCategory.Pain, 4, tags: [], note: null);
        profile.UpsertSymptomEntry(date, CycleSymptomCategory.Mood, 6, tags: [], note: null);

        bool removed = profile.ClearSymptomEntries(date, [CycleSymptomCategory.Pain]);

        Assert.True(removed);
        Assert.Single(profile.BleedingEntries);
        CycleSymptomEntry remaining = Assert.Single(profile.SymptomEntries);
        Assert.Equal(CycleSymptomCategory.Mood, remaining.Category);
    }

    [Fact]
    public void ClearSymptomEntries_WithInvalidCategory_ThrowsWithoutRemovingEntries() {
        DateOnly date = new(2026, 4, 2);
        var profile = CycleProfile.Create(UserId.New(), date.AddDays(-1));
        profile.UpsertSymptomEntry(date, CycleSymptomCategory.Pain, 4, tags: [], note: null);
        DateTime? modifiedAt = profile.ModifiedOnUtc;

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            profile.ClearSymptomEntries(date, [CycleSymptomCategory.Pain, (CycleSymptomCategory)999]));

        Assert.Multiple(
            () => Assert.Single(profile.SymptomEntries),
            () => Assert.Equal(modifiedAt, profile.ModifiedOnUtc));
    }

    [Fact]
    public void ClearSymptomEntries_WithNullCategories_Throws() {
        var profile = CycleProfile.Create(UserId.New(), new DateOnly(2026, 4, 1));

        Assert.Throws<ArgumentNullException>(() =>
            profile.ClearSymptomEntries(new DateOnly(2026, 4, 2), null!));
    }

    [Fact]
    public void ClearFertilitySignal_RemovesOnlyFertilityDataForDate() {
        DateOnly date = new(2026, 4, 2);
        var profile = CycleProfile.Create(UserId.New(), date.AddDays(-1));
        profile.GrantConsent(CycleConsentPurpose.FertilitySignals, DateTime.UnixEpoch);
        profile.UpsertBleedingEntry(date, BleedingType.Bleeding, CycleFlowLevel.Medium, painImpact: 3, notes: null);
        profile.UpsertSymptomEntry(date, CycleSymptomCategory.Pain, 4, tags: [], note: null);
        profile.UpsertFertilitySignal(date, 36.6, OvulationTestResult.Negative, cervicalFluid: null, hadSex: null, notes: null);

        bool removed = profile.ClearFertilitySignal(date);

        Assert.True(removed);
        Assert.Empty(profile.FertilitySignals);
        Assert.Single(profile.BleedingEntries);
        Assert.Single(profile.SymptomEntries);
    }

    [Fact]
    public void UpsertBleedingEntry_GroupsOneUnknownDayIntoOneInferredEpisode() {
        DateOnly start = new(2026, 4, 1);
        var profile = CycleProfile.Create(UserId.New(), start);

        profile.UpsertBleedingEntry(start, BleedingType.Bleeding, CycleFlowLevel.Medium, painImpact: null, notes: null);
        profile.UpsertBleedingEntry(start.AddDays(2), BleedingType.Bleeding, CycleFlowLevel.Light, painImpact: null, notes: null);

        MenstrualEpisode episode = Assert.Single(profile.MenstrualEpisodes);
        Assert.Equal(start, episode.StartDate);
        Assert.Equal(start.AddDays(2), episode.EndDate);
        Assert.Equal(MenstrualEpisodeStatus.Inferred, episode.Status);
    }

    [Fact]
    public void ConfirmPeriodStart_ReplacesOverlappingInferenceAndIsIdempotent() {
        DateOnly start = new(2026, 4, 1);
        var profile = CycleProfile.Create(UserId.New(), start);
        profile.UpsertBleedingEntry(start, BleedingType.Bleeding, CycleFlowLevel.Medium, painImpact: null, notes: null);
        profile.UpsertBleedingEntry(start.AddDays(1), BleedingType.Bleeding, CycleFlowLevel.Light, painImpact: null, notes: null);

        MenstrualEpisode first = profile.ConfirmPeriodStart(start);
        MenstrualEpisode second = profile.ConfirmPeriodStart(start);

        Assert.Same(first, second);
        Assert.Equal(MenstrualEpisodeStatus.Confirmed, Assert.Single(profile.MenstrualEpisodes).Status);
        Assert.Equal(start.AddDays(1), first.EndDate);
    }

    [Fact]
    public void UpdateMenstrualEpisode_ChangesConfirmedRangeAndPreservesDailyObservations() {
        DateOnly start = new(2026, 4, 1);
        var profile = CycleProfile.Create(UserId.New(), start);
        profile.UpsertBleedingEntry(start, BleedingType.Bleeding, CycleFlowLevel.Medium, painImpact: null, notes: "kept");
        MenstrualEpisode episode = profile.ConfirmPeriodStart(start);

        MenstrualEpisode updated = profile.UpdateMenstrualEpisode(episode.Id, start.AddDays(-1), start.AddDays(3));

        Assert.Multiple(
            () => Assert.Same(episode, updated),
            () => Assert.Equal(start.AddDays(-1), updated.StartDate),
            () => Assert.Equal(start.AddDays(3), updated.EndDate),
            () => Assert.Equal("kept", Assert.Single(profile.BleedingEntries).Notes));
    }

    [Fact]
    public void UpdateMenstrualEpisode_WhenConfirmedRangesOverlap_Throws() {
        DateOnly start = new(2026, 4, 1);
        var profile = CycleProfile.Create(UserId.New(), start);
        MenstrualEpisode first = profile.ConfirmPeriodStart(start);
        profile.UpdateMenstrualEpisode(first.Id, start, start.AddDays(4));
        MenstrualEpisode second = profile.ConfirmPeriodStart(start.AddDays(20));

        Assert.Throws<ArgumentException>(() =>
            profile.UpdateMenstrualEpisode(second.Id, start.AddDays(3), start.AddDays(8)));
    }

    [Fact]
    public void UpdateMenstrualEpisode_CanExcludeAndRestoreConfirmedEpisodeFromPredictions() {
        DateOnly start = new(2026, 4, 1);
        var profile = CycleProfile.Create(UserId.New(), start);
        MenstrualEpisode episode = profile.ConfirmPeriodStart(start);

        profile.UpdateMenstrualEpisode(episode.Id, start, start.AddDays(3), excludedFromPredictions: true);
        Assert.True(episode.ExcludedFromPredictions);

        profile.UpdateMenstrualEpisode(episode.Id, start, start.AddDays(3), excludedFromPredictions: false);
        Assert.False(episode.ExcludedFromPredictions);
    }

    [Fact]
    public void RemoveMenstrualEpisode_PreservesDailyFactsAndRebuildsInferredEpisode() {
        DateOnly start = new(2026, 4, 1);
        var profile = CycleProfile.Create(UserId.New(), start);
        profile.UpsertBleedingEntry(start, BleedingType.Bleeding, CycleFlowLevel.Medium, 2, "kept");
        MenstrualEpisode confirmed = profile.ConfirmPeriodStart(start);

        profile.RemoveMenstrualEpisode(confirmed.Id);

        BleedingEntry bleeding = Assert.Single(profile.BleedingEntries);
        MenstrualEpisode inferred = Assert.Single(profile.MenstrualEpisodes);
        Assert.Multiple(
            () => Assert.Equal("kept", bleeding.Notes),
            () => Assert.Equal(start, inferred.StartDate),
            () => Assert.Equal(MenstrualEpisodeStatus.Inferred, inferred.Status));
    }

    [Fact]
    public void UpdateSettings_WithInvalidExplicitGoalOrState_Throws() {
        var profile = CycleProfile.Create(UserId.New(), new DateOnly(2026, 4, 1));

        Assert.Multiple(
            () => Assert.Throws<ArgumentOutOfRangeException>(() => profile.UpdateSettings(new CycleProfileSettings(
                Mode: CycleTrackingMode.PeriodTracking,
                AverageCycleLength: null,
                AveragePeriodLength: null,
                LutealLength: null,
                IsRegular: null,
                IsOnboardingComplete: null,
                ShowFertilityEstimates: null,
                DiscreetNotifications: null,
                Notes: null,
                Goal: (CycleTrackingGoal)int.MaxValue))),
            () => Assert.Throws<ArgumentOutOfRangeException>(() => profile.UpdateSettings(new CycleProfileSettings(
                Mode: CycleTrackingMode.PeriodTracking,
                AverageCycleLength: null,
                AveragePeriodLength: null,
                LutealLength: null,
                IsRegular: null,
                IsOnboardingComplete: null,
                ShowFertilityEstimates: null,
                DiscreetNotifications: null,
                Notes: null,
                ReproductiveState: (CycleReproductiveState)int.MaxValue))));
    }

    [Fact]
    public void ConsentTransitions_CoverRegrantMissingAndFertilityCleanup() {
        var profile = CycleProfile.Create(UserId.New(), new DateOnly(2026, 4, 1));
        DateTime grantedAt = new(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        profile.GrantConsent(CycleConsentPurpose.FertilitySignals, grantedAt);
        profile.UpsertFertilitySignal(
            new DateOnly(2026, 4, 2),
            basalBodyTemperatureCelsius: 36.5,
            ovulationTestResult: null,
            cervicalFluid: null,
            hadSex: null,
            notes: null);
        profile.RevokeConsent(CycleConsentPurpose.FertilitySignals, grantedAt.AddMinutes(1));

        Assert.Empty(profile.FertilitySignals);
        Assert.False(profile.ShowFertilityEstimates);

        profile.GrantConsent(CycleConsentPurpose.FertilitySignals, grantedAt.AddMinutes(2));
        Assert.True(profile.HasActiveConsent(CycleConsentPurpose.FertilitySignals));

        profile.RevokeConsent(CycleConsentPurpose.NutritionInsights, grantedAt);
        Assert.False(profile.HasActiveConsent(CycleConsentPurpose.NutritionInsights));
    }

    [Fact]
    public void ClearOperations_WhenDateHasNoMatchingEntries_ReturnFalse() {
        var profile = CycleProfile.Create(UserId.New(), new DateOnly(2026, 4, 1));
        DateOnly missing = new(2026, 4, 2);

        Assert.Multiple(
            () => Assert.False(profile.ClearBleedingEntries(missing)),
            () => Assert.False(profile.ClearSymptomEntries(missing, [CycleSymptomCategory.Pain])),
            () => Assert.False(profile.ClearFertilitySignal(missing)));
    }

    [Fact]
    public void MenstrualEpisodeOperations_WithEmptyId_Throw() {
        var profile = CycleProfile.Create(UserId.New(), new DateOnly(2026, 4, 1));

        Assert.Multiple(
            () => Assert.Throws<ArgumentException>(() => profile.UpdateMenstrualEpisode(
                MenstrualEpisodeId.Empty,
                new DateOnly(2026, 4, 1),
                endDate: null)),
            () => Assert.Throws<ArgumentException>(() => profile.RemoveMenstrualEpisode(MenstrualEpisodeId.Empty)));
    }

    [Fact]
    public void RemoveMenstrualEpisode_WhenEpisodeIsInferred_Throws() {
        DateOnly date = new(2026, 4, 1);
        var profile = CycleProfile.Create(UserId.New(), date);
        profile.UpsertBleedingEntry(
            date,
            BleedingType.Bleeding,
            CycleFlowLevel.Medium,
            painImpact: null,
            notes: null);
        MenstrualEpisode inferred = Assert.Single(profile.MenstrualEpisodes);

        Assert.Throws<InvalidOperationException>(() => profile.RemoveMenstrualEpisode(inferred.Id));
    }

    [Fact]
    public void UpsertFertilitySignal_WithoutConsent_Throws() {
        var profile = CycleProfile.Create(UserId.New(), new DateOnly(2026, 4, 1));

        Assert.Throws<InvalidOperationException>(() => profile.UpsertFertilitySignal(
            new DateOnly(2026, 4, 2),
            basalBodyTemperatureCelsius: 36.5,
            ovulationTestResult: null,
            cervicalFluid: null,
            hadSex: null,
            notes: null));
    }
}
