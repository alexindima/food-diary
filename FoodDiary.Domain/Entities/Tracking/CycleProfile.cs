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

    public UserId UserId { get; private set; }
    public CycleTrackingMode Mode { get; private set; }
    public CycleConfidence Confidence { get; private set; }
    public DateTime TrackingStartDate { get; private set; }
    public int AverageCycleLength { get; private set; }
    public int AveragePeriodLength { get; private set; }
    public int LutealLength { get; private set; }
    public bool IsRegular { get; private set; }
    public bool IsOnboardingComplete { get; private set; }
    public bool ShowFertilityEstimates { get; private set; }
    public bool DiscreetNotifications { get; private set; }
    public string? Notes { get; private set; }

    public IReadOnlyCollection<CycleFactor> Factors => _factors.AsReadOnly();
    public IReadOnlyCollection<BleedingEntry> BleedingEntries => _bleedingEntries.AsReadOnly();
    public IReadOnlyCollection<CycleSymptomEntry> SymptomEntries => _symptomEntries.AsReadOnly();
    public IReadOnlyCollection<FertilitySignal> FertilitySignals => _fertilitySignals.AsReadOnly();
    public IReadOnlyCollection<MenstrualEpisode> MenstrualEpisodes => _menstrualEpisodes.AsReadOnly();

    private CycleProfile() {
    }

    private CycleProfile(CycleProfileId id) : base(id) {
    }

    public static CycleProfile Create(
        UserId userId,
        DateTime trackingStartDate,
        CycleTrackingMode mode = CycleTrackingMode.PeriodTracking,
        int? averageCycleLength = null,
        int? averagePeriodLength = null,
        int? lutealLength = null,
        bool isRegular = false,
        bool isOnboardingComplete = false,
        bool showFertilityEstimates = false,
        bool discreetNotifications = true,
        string? notes = null) {
        EnsureUserId(userId);
        EnsureDefined(mode, nameof(mode));

        var profile = new CycleProfile(CycleProfileId.New()) {
            UserId = userId,
            TrackingStartDate = NormalizeDate(trackingStartDate),
            Mode = mode,
            Confidence = CycleConfidence.Learning,
            AverageCycleLength = NormalizeCycleLength(averageCycleLength),
            AveragePeriodLength = NormalizePeriodLength(averagePeriodLength),
            LutealLength = NormalizeLutealLength(lutealLength),
            IsRegular = isRegular,
            IsOnboardingComplete = isOnboardingComplete,
            ShowFertilityEstimates = showFertilityEstimates,
            DiscreetNotifications = discreetNotifications,
            Notes = NormalizeNotes(notes),
        };

        profile.SetCreated();
        return profile;
    }

    public void UpdateSettings(CycleProfileSettings settings) {
        ArgumentNullException.ThrowIfNull(settings);
        EnsureDefined(settings.Mode, nameof(settings.Mode));
        EnsureClearConflict(settings.ClearNotes, NormalizeNotes(settings.Notes), nameof(settings.ClearNotes), nameof(settings.Notes));

        Mode = settings.Mode;
        AverageCycleLength = NormalizeCycleLength(settings.AverageCycleLength ?? AverageCycleLength);
        AveragePeriodLength = NormalizePeriodLength(settings.AveragePeriodLength ?? AveragePeriodLength);
        LutealLength = NormalizeLutealLength(settings.LutealLength ?? LutealLength);
        IsRegular = settings.IsRegular ?? IsRegular;
        IsOnboardingComplete = settings.IsOnboardingComplete ?? IsOnboardingComplete;
        ShowFertilityEstimates = settings.ShowFertilityEstimates ?? ShowFertilityEstimates;
        DiscreetNotifications = settings.DiscreetNotifications ?? DiscreetNotifications;
        if (settings.ClearNotes) {
            Notes = null;
        } else if (settings.Notes is not null) {
            Notes = NormalizeNotes(settings.Notes);
        }
        Confidence = CalculateConfidence();

        SetModified();
    }

    public BleedingEntry UpsertBleedingEntry(
        DateTime date,
        BleedingType type,
        CycleFlowLevel flow,
        int? painImpact,
        string? notes,
        bool clearNotes = false) {
        DateTime normalizedDate = NormalizeDate(date);
        BleedingEntry? existing = _bleedingEntries.FirstOrDefault(entry => entry.Date == normalizedDate && entry.Type == type);
        if (existing is not null) {
            existing.Update(flow, painImpact, notes, clearNotes);
            Confidence = CalculateConfidence();
            SetModified();
            return existing;
        }

        var entry = BleedingEntry.Create(Id, normalizedDate, type, flow, painImpact, notes);
        _bleedingEntries.Add(entry);
        ReconcileMenstrualEpisodes();
        Confidence = CalculateConfidence();
        SetModified();
        return entry;
    }

    public CycleSymptomEntry UpsertSymptomEntry(
        DateTime date,
        CycleSymptomCategory category,
        int intensity,
        IReadOnlyCollection<string> tags,
        string? note,
        bool clearNote = false) {
        DateTime normalizedDate = NormalizeDate(date);
        CycleSymptomEntry? existing = _symptomEntries.FirstOrDefault(entry => entry.Date == normalizedDate && entry.Category == category);
        if (existing is not null) {
            existing.Update(intensity, tags, note, clearNote);
            SetModified();
            return existing;
        }

        var entry = CycleSymptomEntry.Create(Id, normalizedDate, category, intensity, tags, note);
        _symptomEntries.Add(entry);
        SetModified();
        return entry;
    }

    public CycleFactor UpsertFactor(CycleFactorType type, DateTime startDate, DateTime? endDate, string? notes, bool clearNotes = false) {
        DateTime normalizedStart = NormalizeDate(startDate);
        CycleFactor? existing = _factors.FirstOrDefault(factor => factor.Type == type && factor.StartDate == normalizedStart);
        if (existing is not null) {
            existing.Update(endDate, notes, clearNotes);
            Confidence = CalculateConfidence();
            SetModified();
            return existing;
        }

        var factor = CycleFactor.Create(Id, type, normalizedStart, endDate, notes);
        _factors.Add(factor);
        Confidence = CalculateConfidence();
        SetModified();
        return factor;
    }

    public FertilitySignal UpsertFertilitySignal(
        DateTime date,
        double? basalBodyTemperatureCelsius,
        OvulationTestResult? ovulationTestResult,
        string? cervicalFluid,
        bool? hadSex,
        string? notes,
        bool clearNotes = false) {
        DateTime normalizedDate = NormalizeDate(date);
        FertilitySignal? existing = _fertilitySignals.FirstOrDefault(signal => signal.Date == normalizedDate);
        if (existing is not null) {
            existing.Update(basalBodyTemperatureCelsius, ovulationTestResult, cervicalFluid, hadSex, notes, clearNotes);
            Confidence = CalculateConfidence();
            SetModified();
            return existing;
        }

        var signal = FertilitySignal.Create(Id, normalizedDate, basalBodyTemperatureCelsius, ovulationTestResult, cervicalFluid, hadSex, notes);
        _fertilitySignals.Add(signal);
        Confidence = CalculateConfidence();
        SetModified();
        return signal;
    }

    public bool ClearBleedingEntries(DateTime date) {
        DateTime normalizedDate = NormalizeDate(date);
        int removedCount = _bleedingEntries.RemoveAll(entry => entry.Date == normalizedDate);
        if (removedCount == 0) {
            return false;
        }

        Confidence = CalculateConfidence();
        ReconcileMenstrualEpisodes();
        SetModified();
        return true;
    }

    public bool ClearDay(DateTime date) {
        DateTime normalizedDate = NormalizeDate(date);
        int removedCount =
            _bleedingEntries.RemoveAll(entry => entry.Date == normalizedDate) +
            _symptomEntries.RemoveAll(entry => entry.Date == normalizedDate) +
            _fertilitySignals.RemoveAll(signal => signal.Date == normalizedDate);

        if (removedCount == 0) {
            return false;
        }

        Confidence = CalculateConfidence();
        ReconcileMenstrualEpisodes();
        SetModified();
        return true;
    }

    public MenstrualEpisode ConfirmPeriodStart(DateTime date) {
        DateTime normalizedDate = NormalizeDate(date);
        MenstrualEpisode? existing = _menstrualEpisodes.FirstOrDefault(episode =>
            episode.StartDate == normalizedDate && episode.Status == MenstrualEpisodeStatus.Confirmed);
        if (existing is not null) {
            return existing;
        }

        var episode = MenstrualEpisode.Create(
            Id,
            normalizedDate,
            FindInferredEpisodeEnd(normalizedDate),
            MenstrualEpisodeStatus.Confirmed);
        _menstrualEpisodes.Add(episode);
        ReconcileMenstrualEpisodes();
        SetModified();
        return episode;
    }

    public MenstrualEpisode UpdateMenstrualEpisode(MenstrualEpisodeId episodeId, DateTime startDate, DateTime? endDate) {
        if (episodeId == MenstrualEpisodeId.Empty) {
            throw new ArgumentException("Menstrual episode id is required.", nameof(episodeId));
        }

        MenstrualEpisode episode = _menstrualEpisodes.FirstOrDefault(item => item.Id == episodeId)
            ?? throw new KeyNotFoundException($"Menstrual episode {episodeId.Value} was not found.");
        DateTime normalizedStart = NormalizeDate(startDate);
        DateTime? normalizedEnd = endDate.HasValue ? NormalizeDate(endDate.Value) : null;
        DateTime effectiveEnd = normalizedEnd ?? normalizedStart;
        bool overlapsAnotherConfirmedEpisode = _menstrualEpisodes.Any(item =>
            item.Id != episodeId &&
            item.Status == MenstrualEpisodeStatus.Confirmed &&
            normalizedStart <= (item.EndDate ?? item.StartDate) &&
            effectiveEnd >= item.StartDate);
        if (overlapsAnotherConfirmedEpisode) {
            throw new ArgumentException("Confirmed menstrual episodes cannot overlap.", nameof(startDate));
        }

        episode.UpdateConfirmedRange(normalizedStart, normalizedEnd);
        ReconcileMenstrualEpisodes();
        SetModified();
        return episode;
    }

    public void ReconcileMenstrualEpisodes() {
        _menstrualEpisodes.RemoveAll(episode => episode.Status == MenstrualEpisodeStatus.Inferred);
        DateTime[] bleedingDates = [.. _bleedingEntries
            .Where(entry => entry.Type == BleedingType.Bleeding)
            .Select(entry => entry.Date)
            .Distinct()
            .Order()];

        for (int index = 0; index < bleedingDates.Length;) {
            DateTime start = bleedingDates[index];
            DateTime end = start;
            while (++index < bleedingDates.Length && (bleedingDates[index] - end).Days <= 2) {
                end = bleedingDates[index];
            }

            bool overlapsConfirmed = _menstrualEpisodes.Any(episode =>
                episode.Status == MenstrualEpisodeStatus.Confirmed &&
                episode.StartDate >= start.AddDays(-2) &&
                episode.StartDate <= end.AddDays(2));
            if (!overlapsConfirmed) {
                _menstrualEpisodes.Add(MenstrualEpisode.Create(Id, start, end, MenstrualEpisodeStatus.Inferred));
            }
        }
    }

    private DateTime? FindInferredEpisodeEnd(DateTime startDate) =>
        _menstrualEpisodes
            .Where(episode => episode.Status == MenstrualEpisodeStatus.Inferred && startDate >= episode.StartDate && startDate <= episode.EndDate)
            .Select(episode => episode.EndDate)
            .FirstOrDefault();

    public DateTime? GetLastBleedingStart() =>
        _bleedingEntries
            .Where(entry => entry.Type == BleedingType.Bleeding)
            .OrderByDescending(entry => entry.Date)
            .Select(entry => (DateTime?)entry.Date)
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

    public static DateTime NormalizeDate(DateTime value) {
        return DateTime.SpecifyKind(value.Kind == DateTimeKind.Unspecified ? value.Date : value.ToUniversalTime().Date, DateTimeKind.Utc);
    }

    internal static string? NormalizeNotes(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

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
