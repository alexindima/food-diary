using FoodDiary.Domain.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Tracking.Fasting;

public sealed class FastingTelemetryEvent : Entity<FastingTelemetryEventId> {
    private const int NameMaxLength = 64;
    private const int SessionIdMaxLength = 64;
    private const int ProtocolMaxLength = 32;
    private const int PlanTypeMaxLength = 16;
    private const int StatusMaxLength = 16;
    private const int OccurrenceKindMaxLength = 16;
    private const int ReminderPresetIdMaxLength = 32;
    private const int ReminderSourceMaxLength = 16;

    public DateTime OccurredAtUtc { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? SessionId { get; private set; }
    public string? Protocol { get; private set; }
    public string? PlanType { get; private set; }
    public string? Status { get; private set; }
    public string? OccurrenceKind { get; private set; }
    public string? ReminderPresetId { get; private set; }
    public string? ReminderSource { get; private set; }
    public int? FirstReminderHours { get; private set; }
    public int? FollowUpReminderHours { get; private set; }
    public int? PlannedDurationHours { get; private set; }
    public double? ActualDurationHours { get; private set; }
    public int? HungerLevel { get; private set; }
    public int? EnergyLevel { get; private set; }
    public int? MoodLevel { get; private set; }
    public int? SymptomsCount { get; private set; }
    public bool? HadNotes { get; private set; }

    private FastingTelemetryEvent() {
    }

    public static FastingTelemetryEvent Create(
        string name,
        DateTime occurredAtUtc,
        string? sessionId = null,
        string? protocol = null,
        string? planType = null,
        string? status = null,
        string? occurrenceKind = null,
        string? reminderPresetId = null,
        string? reminderSource = null,
        int? firstReminderHours = null,
        int? followUpReminderHours = null,
        int? plannedDurationHours = null,
        double? actualDurationHours = null,
        int? hungerLevel = null,
        int? energyLevel = null,
        int? moodLevel = null,
        int? symptomsCount = null,
        bool? hadNotes = null) {
        var entity = new FastingTelemetryEvent {
            Id = FastingTelemetryEventId.New(),
            OccurredAtUtc = NormalizeUtc(occurredAtUtc, nameof(occurredAtUtc)),
            Name = NormalizeRequired(name, NameMaxLength, nameof(name)),
            SessionId = NormalizeOptional(sessionId, SessionIdMaxLength),
            Protocol = NormalizeOptional(protocol, ProtocolMaxLength),
            PlanType = NormalizeOptional(planType, PlanTypeMaxLength),
            Status = NormalizeOptional(status, StatusMaxLength),
            OccurrenceKind = NormalizeOptional(occurrenceKind, OccurrenceKindMaxLength),
            ReminderPresetId = NormalizeOptional(reminderPresetId, ReminderPresetIdMaxLength),
            ReminderSource = NormalizeOptional(reminderSource, ReminderSourceMaxLength),
            FirstReminderHours = NormalizeHours(firstReminderHours, nameof(firstReminderHours)),
            FollowUpReminderHours = NormalizeHours(followUpReminderHours, nameof(followUpReminderHours)),
            PlannedDurationHours = NormalizeHours(plannedDurationHours, nameof(plannedDurationHours)),
            ActualDurationHours = actualDurationHours,
            HungerLevel = NormalizeScale(hungerLevel, nameof(hungerLevel)),
            EnergyLevel = NormalizeScale(energyLevel, nameof(energyLevel)),
            MoodLevel = NormalizeScale(moodLevel, nameof(moodLevel)),
            SymptomsCount = NormalizeNonNegative(symptomsCount, nameof(symptomsCount)),
            HadNotes = hadNotes,
        };

        entity.SetCreated(occurredAtUtc);
        return entity;
    }

    private static string NormalizeRequired(string value, int maxLength, string paramName) {
        var normalized = NormalizeOptional(value, maxLength);
        return normalized ?? throw new ArgumentException("Value is required.", paramName);
    }

    private static string? NormalizeOptional(string? value, int maxLength) {
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength) {
            throw new ArgumentOutOfRangeException(nameof(value), $"Value must be at most {maxLength} characters.");
        }

        return normalized;
    }

    private static int? NormalizeHours(int? value, string paramName) {
        if (!value.HasValue) {
            return null;
        }

        if (value.Value < 1 || value.Value > 168) {
            throw new ArgumentOutOfRangeException(paramName, "Value must be between 1 and 168.");
        }

        return value.Value;
    }

    private static int? NormalizeScale(int? value, string paramName) {
        if (!value.HasValue) {
            return null;
        }

        if (value.Value < 1 || value.Value > 5) {
            throw new ArgumentOutOfRangeException(paramName, "Value must be between 1 and 5.");
        }

        return value.Value;
    }

    private static int? NormalizeNonNegative(int? value, string paramName) {
        if (!value.HasValue) {
            return null;
        }

        if (value.Value < 0) {
            throw new ArgumentOutOfRangeException(paramName, "Value must be non-negative.");
        }

        return value.Value;
    }

    private static DateTime NormalizeUtc(DateTime value, string paramName) {
        if (value.Kind == DateTimeKind.Unspecified) {
            throw new ArgumentOutOfRangeException(paramName, "UTC timestamp kind must be specified.");
        }

        return value.ToUniversalTime();
    }
}
