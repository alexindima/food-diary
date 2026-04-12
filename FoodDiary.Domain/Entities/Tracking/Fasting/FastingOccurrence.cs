using FoodDiary.Domain.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Tracking.Fasting;

public sealed class FastingOccurrence : AggregateRoot<FastingOccurrenceId> {
    private const int NotesMaxLength = 500;
    private const int CheckInNotesMaxLength = 500;
    private const int CheckInSymptomsMaxLength = 200;
    private const int MinCheckInScale = 1;
    private const int MaxCheckInScale = 5;
    private const int MaxSymptomsCount = 8;
    private const int MaxTargetHours = 168;

    public FastingPlanId PlanId { get; private set; }
    public UserId UserId { get; private set; }
    public FastingOccurrenceKind Kind { get; private set; }
    public FastingOccurrenceStatus Status { get; private set; }
    public int SequenceNumber { get; private set; }
    public DateTime? ScheduledForUtc { get; private set; }
    public DateTime StartedAtUtc { get; private set; }
    public DateTime? EndedAtUtc { get; private set; }
    public int? InitialTargetHours { get; private set; }
    public int AddedTargetHours { get; private set; }
    public int? TargetHours => InitialTargetHours.HasValue ? InitialTargetHours.Value + AddedTargetHours : null;
    public string? Notes { get; private set; }
    public DateTime? CheckInAtUtc { get; private set; }
    public int? HungerLevel { get; private set; }
    public int? EnergyLevel { get; private set; }
    public int? MoodLevel { get; private set; }
    public string? Symptoms { get; private set; }
    public string? CheckInNotes { get; private set; }

    public FastingPlan Plan { get; private set; } = null!;
    public User User { get; private set; } = null!;

    private FastingOccurrence() {
    }

    public static FastingOccurrence Create(
        FastingPlanId planId,
        UserId userId,
        FastingOccurrenceKind kind,
        DateTime startedAtUtc,
        int sequenceNumber,
        int? targetHours = null,
        DateTime? scheduledForUtc = null,
        string? notes = null) {
        EnsurePlanId(planId);
        EnsureUserId(userId);
        EnsureSequenceNumber(sequenceNumber);
        EnsureTargetHours(targetHours);

        var occurrence = new FastingOccurrence {
            Id = FastingOccurrenceId.New(),
            PlanId = planId,
            UserId = userId,
            Kind = kind,
            Status = FastingOccurrenceStatus.Active,
            SequenceNumber = sequenceNumber,
            ScheduledForUtc = scheduledForUtc.HasValue ? NormalizeTimestamp(scheduledForUtc.Value, nameof(scheduledForUtc)) : null,
            StartedAtUtc = NormalizeTimestamp(startedAtUtc, nameof(startedAtUtc)),
            EndedAtUtc = null,
            InitialTargetHours = targetHours,
            AddedTargetHours = 0,
            Notes = NormalizeNotes(notes)
        };

        occurrence.SetCreated();
        return occurrence;
    }

    public static FastingOccurrence Schedule(
        FastingPlanId planId,
        UserId userId,
        FastingOccurrenceKind kind,
        DateTime scheduledForUtc,
        int sequenceNumber,
        int? targetHours = null,
        string? notes = null) {
        EnsurePlanId(planId);
        EnsureUserId(userId);
        EnsureSequenceNumber(sequenceNumber);
        EnsureTargetHours(targetHours);

        var normalizedScheduledFor = NormalizeTimestamp(scheduledForUtc, nameof(scheduledForUtc));
        var occurrence = new FastingOccurrence {
            Id = FastingOccurrenceId.New(),
            PlanId = planId,
            UserId = userId,
            Kind = kind,
            Status = FastingOccurrenceStatus.Scheduled,
            SequenceNumber = sequenceNumber,
            ScheduledForUtc = normalizedScheduledFor,
            StartedAtUtc = normalizedScheduledFor,
            EndedAtUtc = null,
            InitialTargetHours = targetHours,
            AddedTargetHours = 0,
            Notes = NormalizeNotes(notes)
        };

        occurrence.SetCreated();
        return occurrence;
    }

    public void Start(DateTime startedAtUtc) {
        if (Status == FastingOccurrenceStatus.Active) {
            return;
        }

        if (Status != FastingOccurrenceStatus.Scheduled && Status != FastingOccurrenceStatus.Postponed) {
            throw new InvalidOperationException("Only scheduled or postponed occurrences can be started.");
        }

        StartedAtUtc = NormalizeTimestamp(startedAtUtc, nameof(startedAtUtc));
        Status = FastingOccurrenceStatus.Active;
        EndedAtUtc = null;
        SetModified();
    }

    public void Complete(DateTime endedAtUtc) {
        EnsureTerminalTransition();
        EndedAtUtc = NormalizeTimestamp(endedAtUtc, nameof(endedAtUtc));
        Status = FastingOccurrenceStatus.Completed;
        SetModified();
    }

    public void Interrupt(DateTime endedAtUtc) {
        EnsureTerminalTransition();
        EndedAtUtc = NormalizeTimestamp(endedAtUtc, nameof(endedAtUtc));
        Status = FastingOccurrenceStatus.Interrupted;
        SetModified();
    }

    public void Skip(DateTime endedAtUtc) {
        EnsureTerminalTransition();
        EndedAtUtc = NormalizeTimestamp(endedAtUtc, nameof(endedAtUtc));
        Status = FastingOccurrenceStatus.Skipped;
        SetModified();
    }

    public void Postpone(DateTime postponedAtUtc, DateTime nextScheduledForUtc) {
        EnsureTerminalTransition();

        var normalizedPostponedAt = NormalizeTimestamp(postponedAtUtc, nameof(postponedAtUtc));
        var normalizedNextScheduledFor = NormalizeTimestamp(nextScheduledForUtc, nameof(nextScheduledForUtc));
        if (normalizedNextScheduledFor <= normalizedPostponedAt) {
            throw new ArgumentOutOfRangeException(nameof(nextScheduledForUtc), "The next scheduled time must be later than the postponement time.");
        }

        EndedAtUtc = normalizedPostponedAt;
        ScheduledForUtc = normalizedNextScheduledFor;
        Status = FastingOccurrenceStatus.Postponed;
        SetModified();
    }

    public void Extend(int additionalHours) {
        if (Status != FastingOccurrenceStatus.Active) {
            throw new InvalidOperationException("Only active occurrences can be extended.");
        }

        if (!InitialTargetHours.HasValue) {
            throw new InvalidOperationException("This occurrence does not have a target duration.");
        }

        if (additionalHours <= 0) {
            throw new ArgumentOutOfRangeException(nameof(additionalHours), "Additional hours must be greater than zero.");
        }

        EnsureTargetHours(TargetHours.GetValueOrDefault() + additionalHours);
        AddedTargetHours += additionalHours;
        SetModified();
    }

    public void Reduce(int reducedHours) {
        if (Status != FastingOccurrenceStatus.Active) {
            throw new InvalidOperationException("Only active occurrences can be adjusted.");
        }

        if (!InitialTargetHours.HasValue) {
            throw new InvalidOperationException("This occurrence does not have a target duration.");
        }

        if (reducedHours <= 0) {
            throw new ArgumentOutOfRangeException(nameof(reducedHours), "Reduced hours must be greater than zero.");
        }

        EnsureTargetHours(TargetHours.GetValueOrDefault() - reducedHours);
        AddedTargetHours -= reducedHours;
        SetModified();
    }

    public void UpdateNotes(string? notes) {
        var normalizedNotes = NormalizeNotes(notes);
        if (Notes == normalizedNotes) {
            return;
        }

        Notes = normalizedNotes;
        SetModified();
    }

    public void UpdateCheckIn(
        int hungerLevel,
        int energyLevel,
        int moodLevel,
        IEnumerable<string>? symptoms,
        string? checkInNotes,
        DateTime checkedInAtUtc) {
        EnsureCheckInScale(hungerLevel, nameof(hungerLevel));
        EnsureCheckInScale(energyLevel, nameof(energyLevel));
        EnsureCheckInScale(moodLevel, nameof(moodLevel));

        HungerLevel = hungerLevel;
        EnergyLevel = energyLevel;
        MoodLevel = moodLevel;
        Symptoms = NormalizeSymptoms(symptoms);
        CheckInNotes = NormalizeCheckInNotes(checkInNotes);
        CheckInAtUtc = NormalizeTimestamp(checkedInAtUtc, nameof(checkedInAtUtc));
        SetModified();
    }

    private void EnsureTerminalTransition() {
        if (Status is FastingOccurrenceStatus.Completed or FastingOccurrenceStatus.Interrupted or FastingOccurrenceStatus.Skipped) {
            throw new InvalidOperationException("Cannot change a finalized fasting occurrence.");
        }
    }

    private static void EnsurePlanId(FastingPlanId planId) {
        if (planId == FastingPlanId.Empty) {
            throw new ArgumentException("PlanId is required.", nameof(planId));
        }
    }

    private static void EnsureUserId(UserId userId) {
        if (userId == UserId.Empty) {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }
    }

    private static void EnsureSequenceNumber(int sequenceNumber) {
        if (sequenceNumber <= 0) {
            throw new ArgumentOutOfRangeException(nameof(sequenceNumber), "Sequence number must be greater than zero.");
        }
    }

    private static void EnsureTargetHours(int? targetHours) {
        if (!targetHours.HasValue) {
            return;
        }

        if (targetHours.Value <= 0 || targetHours.Value > MaxTargetHours) {
            throw new ArgumentOutOfRangeException(nameof(targetHours), $"Target hours must be between 1 and {MaxTargetHours}.");
        }
    }

    private static DateTime NormalizeTimestamp(DateTime value, string paramName) {
        if (value.Kind == DateTimeKind.Unspecified) {
            throw new ArgumentOutOfRangeException(paramName, "UTC timestamp kind must be specified.");
        }

        return value.ToUniversalTime();
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

    private static void EnsureCheckInScale(int value, string paramName) {
        if (value < MinCheckInScale || value > MaxCheckInScale) {
            throw new ArgumentOutOfRangeException(paramName, $"Check-in value must be between {MinCheckInScale} and {MaxCheckInScale}.");
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
        return csv.Length > CheckInSymptomsMaxLength
            ? throw new ArgumentOutOfRangeException(nameof(values), $"Symptoms must be at most {CheckInSymptomsMaxLength} characters in total.")
            : csv;
    }

    private static string? NormalizeCheckInNotes(string? value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length > CheckInNotesMaxLength
            ? throw new ArgumentOutOfRangeException(nameof(value), $"Check-in notes must be at most {CheckInNotesMaxLength} characters.")
            : trimmed;
    }
}
