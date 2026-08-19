using FoodDiary.Domain.Primitives;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Tracking;

public sealed class CycleProfile : AggregateRoot<CycleProfileId> {
    private const int DefaultCycleLength = 28;
    private const int DefaultPeriodLength = 5;
    private const int DefaultLutealLength = 14;

    private readonly List<CycleFactor> _factors = [];
    private readonly List<BleedingEntry> _bleedingEntries = [];
    private readonly List<CycleSymptomEntry> _symptomEntries = [];
    private readonly List<FertilitySignal> _fertilitySignals = [];
    private readonly List<MenstrualEpisode> _menstrualEpisodes = [];
    private readonly List<CycleConsent> _consents = [];
    private readonly List<CyclePredictionRevision> _predictionRevisions = [];

    public UserId UserId { get; private set; }
    public CycleTrackingMode Mode { get; private set; }
    public CycleTrackingGoal Goal { get; private set; }
    public CycleReproductiveState ReproductiveState { get; private set; }
    public CycleConfidence Confidence { get; private set; }
    public DateOnly TrackingStartDate { get; private set; }
    public int AverageCycleLength { get; private set; }
    public int AveragePeriodLength { get; private set; }
    public int LutealLength { get; private set; }
    public bool IsRegular { get; private set; }
    public bool IsOnboardingComplete { get; private set; }
    public bool ShowFertilityEstimates { get; private set; }
    public bool DiscreetNotifications { get; private set; }
    public bool HideFromDashboard { get; private set; }
    public string? Notes { get; private set; }

    public IReadOnlyCollection<CycleFactor> Factors => _factors.AsReadOnly();
    public IReadOnlyCollection<BleedingEntry> BleedingEntries => _bleedingEntries.AsReadOnly();
    public IReadOnlyCollection<CycleSymptomEntry> SymptomEntries => _symptomEntries.AsReadOnly();
    public IReadOnlyCollection<FertilitySignal> FertilitySignals => _fertilitySignals.AsReadOnly();
    public IReadOnlyCollection<MenstrualEpisode> MenstrualEpisodes => _menstrualEpisodes.AsReadOnly();
    public IReadOnlyCollection<CycleConsent> Consents => _consents.AsReadOnly();
    public IReadOnlyCollection<CyclePredictionRevision> PredictionRevisions => _predictionRevisions.AsReadOnly();

    private CycleProfile() {
    }

    private CycleProfile(CycleProfileId id) : base(id) {
    }

    public static CycleProfile Create(
        UserId userId,
        DateOnly trackingStartDate,
        CycleTrackingMode mode = CycleTrackingMode.PeriodTracking,
        int? averageCycleLength = null,
        int? averagePeriodLength = null,
        int? lutealLength = null,
        bool isRegular = false,
        bool isOnboardingComplete = false,
        bool showFertilityEstimates = false,
        bool discreetNotifications = true,
        string? notes = null,
        CycleTrackingGoal? goal = null,
        CycleReproductiveState? reproductiveState = null,
        bool hideFromDashboard = false,
        DateTime? consentGrantedAtUtc = null) {
        EnsureUserId(userId);
        EnsureDefined(mode, nameof(mode));
        if (goal.HasValue) {
            EnsureDefined(goal.Value, nameof(goal));
        }
        if (reproductiveState.HasValue) {
            EnsureDefined(reproductiveState.Value, nameof(reproductiveState));
        }

        var profile = new CycleProfile(CycleProfileId.New()) {
            UserId = userId,
            TrackingStartDate = trackingStartDate,
            Mode = goal.HasValue || reproductiveState.HasValue
                ? ModeFromGoalAndState(goal ?? GoalFromLegacyMode(mode), reproductiveState ?? StateFromLegacyMode(mode))
                : mode,
            Goal = goal ?? GoalFromLegacyMode(mode),
            ReproductiveState = reproductiveState ?? StateFromLegacyMode(mode),
            Confidence = CycleConfidence.Learning,
            AverageCycleLength = NormalizeCycleLength(averageCycleLength),
            AveragePeriodLength = NormalizePeriodLength(averagePeriodLength),
            LutealLength = NormalizeLutealLength(lutealLength),
            IsRegular = isRegular,
            IsOnboardingComplete = isOnboardingComplete,
            ShowFertilityEstimates = showFertilityEstimates,
            DiscreetNotifications = discreetNotifications,
            HideFromDashboard = hideFromDashboard,
            Notes = NormalizeNotes(notes),
        };

        profile._consents.Add(CycleConsent.Create(
            profile.Id,
            CycleConsentPurpose.CycleTracking,
            consentGrantedAtUtc ?? DomainTime.UtcNow));

        profile.SetCreated();
        return profile;
    }

    public void UpdateSettings(CycleProfileSettings settings) {
        ArgumentNullException.ThrowIfNull(settings);
        EnsureDefined(settings.Mode, nameof(settings.Mode));
        if (settings.Goal.HasValue) {
            EnsureDefined(settings.Goal.Value, nameof(settings.Goal));
        }
        if (settings.ReproductiveState.HasValue) {
            EnsureDefined(settings.ReproductiveState.Value, nameof(settings.ReproductiveState));
        }
        string? normalizedNotes = NormalizeNotes(settings.Notes);
        EnsureClearConflict(settings.ClearNotes, normalizedNotes, nameof(settings.ClearNotes), nameof(settings.Notes));

        CycleTrackingGoal goal = settings.Goal ?? Goal;
        CycleReproductiveState reproductiveState = settings.ReproductiveState ?? ReproductiveState;
        CycleTrackingMode mode = settings.Goal.HasValue || settings.ReproductiveState.HasValue
            ? ModeFromGoalAndState(goal, reproductiveState)
            : settings.Mode;
        int averageCycleLength = NormalizeCycleLength(settings.AverageCycleLength ?? AverageCycleLength);
        int averagePeriodLength = NormalizePeriodLength(settings.AveragePeriodLength ?? AveragePeriodLength);
        int lutealLength = NormalizeLutealLength(settings.LutealLength ?? LutealLength);

        Goal = goal;
        ReproductiveState = reproductiveState;
        Mode = mode;
        AverageCycleLength = averageCycleLength;
        AveragePeriodLength = averagePeriodLength;
        LutealLength = lutealLength;
        IsRegular = settings.IsRegular ?? IsRegular;
        IsOnboardingComplete = settings.IsOnboardingComplete ?? IsOnboardingComplete;
        ShowFertilityEstimates = settings.ShowFertilityEstimates ?? ShowFertilityEstimates;
        DiscreetNotifications = settings.DiscreetNotifications ?? DiscreetNotifications;
        HideFromDashboard = settings.HideFromDashboard ?? HideFromDashboard;
        if (settings.ClearNotes) {
            Notes = null;
        } else if (settings.Notes is not null) {
            Notes = normalizedNotes;
        }
        Confidence = CalculateConfidence();

        SetModified();
    }

    public bool HasActiveConsent(CycleConsentPurpose purpose) =>
        _consents.Exists(consent => consent.Purpose == purpose && consent.IsActive);

    public void GrantConsent(CycleConsentPurpose purpose, DateTime grantedAtUtc) {
        EnsureDefined(purpose, nameof(purpose));
        CycleConsent? consent = _consents.FirstOrDefault(item => item.Purpose == purpose);
        if (consent is null) {
            _consents.Add(CycleConsent.Create(Id, purpose, grantedAtUtc));
            SetModified();
            return;
        }

        if (consent.Grant(grantedAtUtc)) {
            SetModified();
        }
    }

    public void RevokeConsent(CycleConsentPurpose purpose, DateTime revokedAtUtc) {
        EnsureDefined(purpose, nameof(purpose));
        CycleConsent? consent = _consents.FirstOrDefault(item => item.Purpose == purpose && item.IsActive);
        if (consent is null) {
            return;
        }

        consent.Revoke(revokedAtUtc);
        if (purpose == CycleConsentPurpose.FertilitySignals) {
            _fertilitySignals.Clear();
            ShowFertilityEstimates = false;
        }

        SetModified();
    }

    public void RecordPredictionRevision(
        DateTime generatedAtUtc,
        DateOnly? nextPeriodStartFrom,
        DateOnly? nextPeriodStartTo,
        string confidence,
        string dataSufficiency,
        string patternConsistency,
        int completedCycleCount,
        int calibrationSampleCount,
        double? historicalCoveragePercent,
        double? meanAbsoluteErrorDays,
        IReadOnlyCollection<string> reasonCodes,
        string algorithmVersion) {
        _predictionRevisions.Add(CyclePredictionRevision.Create(
            Id,
            generatedAtUtc,
            nextPeriodStartFrom,
            nextPeriodStartTo,
            confidence,
            dataSufficiency,
            patternConsistency,
            completedCycleCount,
            calibrationSampleCount,
            historicalCoveragePercent,
            meanAbsoluteErrorDays,
            reasonCodes,
            algorithmVersion));
        SetModified();
    }

    public BleedingEntry UpsertBleedingEntry(
        DateOnly date,
        BleedingType type,
        CycleFlowLevel flow,
        int? painImpact,
        string? notes,
        bool clearNotes = false) {
        BleedingEntry? existing = _bleedingEntries.FirstOrDefault(entry => entry.Date == date && entry.Type == type);
        if (existing is not null) {
            existing.Update(flow, painImpact, notes, clearNotes);
            Confidence = CalculateConfidence();
            SetModified();
            return existing;
        }

        var entry = BleedingEntry.Create(Id, date, type, flow, painImpact, notes);
        _bleedingEntries.Add(entry);
        ReconcileMenstrualEpisodes();
        Confidence = CalculateConfidence();
        SetModified();
        return entry;
    }

    public CycleSymptomEntry UpsertSymptomEntry(
        DateOnly date,
        CycleSymptomCategory category,
        int intensity,
        IReadOnlyCollection<string> tags,
        string? note,
        bool clearNote = false) {
        CycleSymptomEntry? existing = _symptomEntries.FirstOrDefault(entry => entry.Date == date && entry.Category == category);
        if (existing is not null) {
            existing.Update(intensity, tags, note, clearNote);
            SetModified();
            return existing;
        }

        var entry = CycleSymptomEntry.Create(Id, date, category, intensity, tags, note);
        _symptomEntries.Add(entry);
        SetModified();
        return entry;
    }

    public CycleFactor UpsertFactor(CycleFactorType type, DateOnly startDate, DateOnly? endDate, string? notes, bool clearNotes = false) {
        CycleFactor? existing = _factors.FirstOrDefault(factor => factor.Type == type && factor.StartDate == startDate);
        if (existing is not null) {
            existing.Update(endDate, notes, clearNotes);
            Confidence = CalculateConfidence();
            SetModified();
            return existing;
        }

        var factor = CycleFactor.Create(Id, type, startDate, endDate, notes);
        _factors.Add(factor);
        Confidence = CalculateConfidence();
        SetModified();
        return factor;
    }

    public FertilitySignal UpsertFertilitySignal(
        DateOnly date,
        double? basalBodyTemperatureCelsius,
        OvulationTestResult? ovulationTestResult,
        string? cervicalFluid,
        bool? hadSex,
        string? notes,
        bool clearNotes = false) {
        if (!HasActiveConsent(CycleConsentPurpose.FertilitySignals)) {
            throw new InvalidOperationException("Active fertility consent is required.");
        }

        FertilitySignal? existing = _fertilitySignals.FirstOrDefault(signal => signal.Date == date);
        if (existing is not null) {
            existing.Update(basalBodyTemperatureCelsius, ovulationTestResult, cervicalFluid, hadSex, notes, clearNotes);
            Confidence = CalculateConfidence();
            SetModified();
            return existing;
        }

        var signal = FertilitySignal.Create(Id, date, basalBodyTemperatureCelsius, ovulationTestResult, cervicalFluid, hadSex, notes);
        _fertilitySignals.Add(signal);
        Confidence = CalculateConfidence();
        SetModified();
        return signal;
    }

    public bool ClearBleedingEntries(DateOnly date) {
        int removedCount = _bleedingEntries.RemoveAll(entry => entry.Date == date);
        if (removedCount == 0) {
            return false;
        }

        Confidence = CalculateConfidence();
        ReconcileMenstrualEpisodes();
        SetModified();
        return true;
    }

    public bool ClearSymptomEntries(DateOnly date, IReadOnlyCollection<CycleSymptomCategory> categories) {
        HashSet<CycleSymptomCategory> categorySet = [.. categories];
        int removedCount = _symptomEntries.RemoveAll(entry => entry.Date == date && categorySet.Contains(entry.Category));
        if (removedCount == 0) {
            return false;
        }

        SetModified();
        return true;
    }

    public bool ClearFertilitySignal(DateOnly date) {
        int removedCount = _fertilitySignals.RemoveAll(signal => signal.Date == date);
        if (removedCount == 0) {
            return false;
        }

        SetModified();
        return true;
    }

    public bool ClearDay(DateOnly date) {
        int removedCount =
            _bleedingEntries.RemoveAll(entry => entry.Date == date) +
            _symptomEntries.RemoveAll(entry => entry.Date == date) +
            _fertilitySignals.RemoveAll(signal => signal.Date == date);

        if (removedCount == 0) {
            return false;
        }

        Confidence = CalculateConfidence();
        ReconcileMenstrualEpisodes();
        SetModified();
        return true;
    }

    public MenstrualEpisode ConfirmPeriodStart(DateOnly date) {
        MenstrualEpisode? existing = _menstrualEpisodes.FirstOrDefault(episode =>
            episode.StartDate == date && episode.Status == MenstrualEpisodeStatus.Confirmed);
        if (existing is not null) {
            return existing;
        }

        DateOnly? inferredEnd = FindInferredEpisodeEnd(date);
        DateOnly effectiveEnd = inferredEnd ?? date;
        bool overlapsConfirmed = _menstrualEpisodes.Exists(episode =>
            episode.Status == MenstrualEpisodeStatus.Confirmed &&
            DateRangesOverlap(date, effectiveEnd, episode.StartDate, episode.EndDate ?? episode.StartDate));
        if (overlapsConfirmed) {
            throw new ArgumentException("Confirmed menstrual episodes cannot overlap.", nameof(date));
        }

        var episode = MenstrualEpisode.Create(
            Id,
            date,
            inferredEnd,
            MenstrualEpisodeStatus.Confirmed);
        _menstrualEpisodes.Add(episode);
        ReconcileMenstrualEpisodes();
        SetModified();
        return episode;
    }

    public MenstrualEpisode UpdateMenstrualEpisode(
        MenstrualEpisodeId episodeId,
        DateOnly startDate,
        DateOnly? endDate,
        bool? excludedFromPredictions = null) {
        if (episodeId == MenstrualEpisodeId.Empty) {
            throw new ArgumentException("Menstrual episode id is required.", nameof(episodeId));
        }

        MenstrualEpisode episode = _menstrualEpisodes.FirstOrDefault(item => item.Id == episodeId)
            ?? throw new KeyNotFoundException($"Menstrual episode {episodeId.Value} was not found.");
        DateOnly effectiveEnd = endDate ?? startDate;
        bool overlapsAnotherConfirmedEpisode = _menstrualEpisodes.Exists(item =>
            item.Id != episodeId &&
            item.Status == MenstrualEpisodeStatus.Confirmed &&
            startDate <= (item.EndDate ?? item.StartDate) &&
            effectiveEnd >= item.StartDate);
        if (overlapsAnotherConfirmedEpisode) {
            throw new ArgumentException("Confirmed menstrual episodes cannot overlap.", nameof(startDate));
        }

        episode.UpdateConfirmedRange(startDate, endDate);
        if (excludedFromPredictions.HasValue) {
            episode.SetPredictionExclusion(excludedFromPredictions.Value);
        }
        ReconcileMenstrualEpisodes();
        SetModified();
        return episode;
    }

    public void RemoveMenstrualEpisode(MenstrualEpisodeId episodeId) {
        if (episodeId == MenstrualEpisodeId.Empty) {
            throw new ArgumentException("Menstrual episode id is required.", nameof(episodeId));
        }

        MenstrualEpisode episode = _menstrualEpisodes.FirstOrDefault(item => item.Id == episodeId)
            ?? throw new KeyNotFoundException($"Menstrual episode {episodeId.Value} was not found.");
        if (episode.Status != MenstrualEpisodeStatus.Confirmed) {
            throw new InvalidOperationException("Only confirmed menstrual episodes can be removed.");
        }

        _menstrualEpisodes.Remove(episode);
        ReconcileMenstrualEpisodes();
        SetModified();
    }

    private void ReconcileMenstrualEpisodes() {
        _menstrualEpisodes.RemoveAll(episode => episode.Status == MenstrualEpisodeStatus.Inferred);
        DateOnly[] bleedingDates = [.. _bleedingEntries
            .Where(entry => entry.Type == BleedingType.Bleeding)
            .Select(entry => entry.Date)
            .Distinct()
            .Order()];

        for (int index = 0; index < bleedingDates.Length;) {
            DateOnly start = bleedingDates[index];
            DateOnly end = start;
            while (++index < bleedingDates.Length && bleedingDates[index].DayNumber - end.DayNumber <= 2) {
                end = bleedingDates[index];
            }

            bool overlapsConfirmed = _menstrualEpisodes.Exists(episode =>
                episode.Status == MenstrualEpisodeStatus.Confirmed &&
                RangesWithinTolerance(
                    start,
                    end,
                    episode.StartDate,
                    episode.EndDate ?? episode.StartDate,
                    toleranceDays: 2));
            if (!overlapsConfirmed) {
                _menstrualEpisodes.Add(MenstrualEpisode.Create(Id, start, end, MenstrualEpisodeStatus.Inferred));
            }
        }
    }

    private DateOnly? FindInferredEpisodeEnd(DateOnly startDate) =>
        _menstrualEpisodes
            .Where(episode => episode.Status == MenstrualEpisodeStatus.Inferred && startDate >= episode.StartDate && startDate <= episode.EndDate)
            .Select(episode => episode.EndDate)
            .FirstOrDefault();

    public DateOnly? GetLastBleedingStart() =>
        _bleedingEntries
            .Where(entry => entry.Type == BleedingType.Bleeding)
            .OrderByDescending(entry => entry.Date)
            .Select(entry => (DateOnly?)entry.Date)
            .FirstOrDefault();

    private CycleConfidence CalculateConfidence() {
        if (Mode is CycleTrackingMode.Pregnancy or CycleTrackingMode.PostpartumLactation || HasActiveHormonalFactor()) {
            return CycleConfidence.Low;
        }

        int bleedingDays = _bleedingEntries.Count(entry => entry.Type == BleedingType.Bleeding);
        return bleedingDays switch {
            >= 9 when IsRegular => CycleConfidence.High,
            >= 6 => CycleConfidence.Medium,
            >= 3 => CycleConfidence.Low,
            _ => CycleConfidence.Learning,
        };
    }

    private bool HasActiveHormonalFactor() =>
        _factors.Exists(factor => factor is { Type: CycleFactorType.HormonalContraception, EndDate: null });

    private static CycleTrackingGoal GoalFromLegacyMode(CycleTrackingMode mode) =>
        mode == CycleTrackingMode.TryingToConceive
            ? CycleTrackingGoal.TryingToConceive
            : CycleTrackingGoal.PeriodAwareness;

    private static CycleReproductiveState StateFromLegacyMode(CycleTrackingMode mode) =>
        mode switch {
            CycleTrackingMode.Pregnancy => CycleReproductiveState.Pregnancy,
            CycleTrackingMode.PostpartumLactation => CycleReproductiveState.Postpartum,
            CycleTrackingMode.Perimenopause => CycleReproductiveState.Perimenopause,
            CycleTrackingMode.NoPeriod => CycleReproductiveState.NoPeriod,
            _ => CycleReproductiveState.Cycling,
        };

    private static CycleTrackingMode ModeFromGoalAndState(
        CycleTrackingGoal goal,
        CycleReproductiveState reproductiveState) =>
        reproductiveState switch {
            CycleReproductiveState.Pregnancy => CycleTrackingMode.Pregnancy,
            CycleReproductiveState.Postpartum or CycleReproductiveState.Lactation => CycleTrackingMode.PostpartumLactation,
            CycleReproductiveState.Perimenopause => CycleTrackingMode.Perimenopause,
            CycleReproductiveState.NoPeriod => CycleTrackingMode.NoPeriod,
            _ when goal == CycleTrackingGoal.TryingToConceive => CycleTrackingMode.TryingToConceive,
            _ => CycleTrackingMode.PeriodTracking,
        };

    internal static string? NormalizeNotes(string? value) {
        const int maxLength = 1024;
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }

        string normalized = value.Trim();
        return normalized.Length > maxLength
            ? throw new ArgumentOutOfRangeException(nameof(value), $"Notes must be at most {maxLength} characters.")
            : normalized;
    }

    private static bool DateRangesOverlap(
        DateOnly leftStart,
        DateOnly leftEnd,
        DateOnly rightStart,
        DateOnly rightEnd) {
        return leftStart.DayNumber <= rightEnd.DayNumber && leftEnd.DayNumber >= rightStart.DayNumber;
    }

    private static bool RangesWithinTolerance(
        DateOnly leftStart,
        DateOnly leftEnd,
        DateOnly rightStart,
        DateOnly rightEnd,
        int toleranceDays) {
        return leftStart.DayNumber <= rightEnd.DayNumber + toleranceDays &&
               leftEnd.DayNumber >= rightStart.DayNumber - toleranceDays;
    }

    internal static int NormalizeIntensity(int value, string paramName) =>
        value is < 0 or > 10 ? throw new ArgumentOutOfRangeException(paramName, "Value must be in range [0, 10].") : value;

    private static int NormalizeCycleLength(int? value) {
        int length = value ?? DefaultCycleLength;
        return length is < 18 or > 60
            ? throw new ArgumentOutOfRangeException(nameof(value), "Average cycle length must be in range [18, 60].")
            : length;
    }

    private static int NormalizePeriodLength(int? value) {
        int length = value ?? DefaultPeriodLength;
        return length is < 1 or > 14
            ? throw new ArgumentOutOfRangeException(nameof(value), "Average period length must be in range [1, 14].")
            : length;
    }

    private static int NormalizeLutealLength(int? value) {
        int length = value ?? DefaultLutealLength;
        return length is < 8 or > 18
            ? throw new ArgumentOutOfRangeException(nameof(value), "Luteal length must be in range [8, 18].")
            : length;
    }

    private static void EnsureUserId(UserId userId) {
        if (userId == UserId.Empty) {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }
    }

    private static void EnsureDefined<TEnum>(TEnum value, string paramName)
        where TEnum : struct, Enum {
        if (!Enum.IsDefined(value)) {
            throw new ArgumentOutOfRangeException(paramName, "Value must be one of the supported values.");
        }
    }

    private static void EnsureClearConflict<T>(bool clear, T? value, string clearParamName, string valueParamName)
        where T : class {
        if (clear && value is not null) {
            throw new ArgumentException($"{clearParamName} cannot be true when {valueParamName} is provided.", clearParamName);
        }
    }
}
