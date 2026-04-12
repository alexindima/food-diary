using FoodDiary.Domain.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Tracking.Fasting;

public sealed class FastingCheckIn : Entity<FastingCheckInId> {
    private const int NotesMaxLength = 500;
    private const int SymptomsMaxLength = 200;
    private const int MinScale = 1;
    private const int MaxScale = 5;
    private const int MaxSymptomsCount = 8;

    public FastingOccurrenceId OccurrenceId { get; private set; }
    public UserId UserId { get; private set; }
    public DateTime CheckedInAtUtc { get; private set; }
    public int HungerLevel { get; private set; }
    public int EnergyLevel { get; private set; }
    public int MoodLevel { get; private set; }
    public string? Symptoms { get; private set; }
    public string? Notes { get; private set; }

    public FastingOccurrence Occurrence { get; private set; } = null!;

    private FastingCheckIn() {
    }

    public static FastingCheckIn Create(
        FastingOccurrenceId occurrenceId,
        UserId userId,
        int hungerLevel,
        int energyLevel,
        int moodLevel,
        IEnumerable<string>? symptoms,
        string? notes,
        DateTime checkedInAtUtc) {
        EnsureOccurrenceId(occurrenceId);
        EnsureUserId(userId);
        EnsureScale(hungerLevel, nameof(hungerLevel));
        EnsureScale(energyLevel, nameof(energyLevel));
        EnsureScale(moodLevel, nameof(moodLevel));

        var entity = new FastingCheckIn {
            Id = FastingCheckInId.New(),
            OccurrenceId = occurrenceId,
            UserId = userId,
            CheckedInAtUtc = NormalizeUtc(checkedInAtUtc, nameof(checkedInAtUtc)),
            HungerLevel = hungerLevel,
            EnergyLevel = energyLevel,
            MoodLevel = moodLevel,
            Symptoms = NormalizeSymptoms(symptoms),
            Notes = NormalizeNotes(notes),
        };

        entity.SetCreated(checkedInAtUtc);
        return entity;
    }

    private static void EnsureOccurrenceId(FastingOccurrenceId occurrenceId) {
        if (occurrenceId == FastingOccurrenceId.Empty) {
            throw new ArgumentException("OccurrenceId is required.", nameof(occurrenceId));
        }
    }

    private static void EnsureUserId(UserId userId) {
        if (userId == UserId.Empty) {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }
    }

    private static void EnsureScale(int value, string paramName) {
        if (value < MinScale || value > MaxScale) {
            throw new ArgumentOutOfRangeException(paramName, $"Value must be between {MinScale} and {MaxScale}.");
        }
    }

    private static string? NormalizeSymptoms(IEnumerable<string>? values) {
        if (values is null) {
            return null;
        }

        var normalized = values
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (normalized.Length == 0) {
            return null;
        }

        if (normalized.Length > MaxSymptomsCount) {
            throw new ArgumentOutOfRangeException(nameof(values), $"A maximum of {MaxSymptomsCount} symptoms is allowed.");
        }

        var csv = string.Join(',', normalized);
        return csv.Length > SymptomsMaxLength
            ? throw new ArgumentOutOfRangeException(nameof(values), $"Symptoms must be at most {SymptomsMaxLength} characters in total.")
            : csv;
    }

    private static string? NormalizeNotes(string? value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length > NotesMaxLength
            ? throw new ArgumentOutOfRangeException(nameof(value), $"Notes must be at most {NotesMaxLength} characters.")
            : trimmed;
    }

    private static DateTime NormalizeUtc(DateTime value, string paramName) {
        if (value.Kind == DateTimeKind.Unspecified) {
            throw new ArgumentOutOfRangeException(paramName, "UTC timestamp kind must be specified.");
        }

        return value.ToUniversalTime();
    }
}
