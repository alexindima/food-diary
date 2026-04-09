using FoodDiary.Domain.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Tracking;

public sealed class FastingSession : AggregateRoot<FastingSessionId> {
    private const int NotesMaxLength = 500;
    private const int MinDurationHours = 1;
    private const int MaxDurationHours = 168;

    public UserId UserId { get; private set; }
    public DateTime StartedAtUtc { get; private set; }
    public DateTime? EndedAtUtc { get; private set; }
    public int InitialPlannedDurationHours { get; private set; }
    public int AddedDurationHours { get; private set; }
    public int PlannedDurationHours => InitialPlannedDurationHours + AddedDurationHours;
    public FastingProtocol Protocol { get; private set; }
    public bool IsCompleted { get; private set; }
    public string? Notes { get; private set; }
    public FastingSessionStatus Status => GetStatus();
    public bool IsSuccessfulCompletion => GetStatus() == FastingSessionStatus.Completed;

    public User User { get; private set; } = null!;

    private FastingSession() {
    }

    public static FastingSession Create(
        UserId userId,
        FastingProtocol protocol,
        int plannedDurationHours,
        DateTime startedAtUtc,
        string? notes = null) {
        EnsureUserId(userId);
        EnsureDuration(plannedDurationHours);

        var session = new FastingSession {
            Id = FastingSessionId.New(),
            UserId = userId,
            StartedAtUtc = startedAtUtc,
            InitialPlannedDurationHours = plannedDurationHours,
            AddedDurationHours = 0,
            Protocol = protocol,
            IsCompleted = false,
            Notes = NormalizeNotes(notes),
        };
        session.SetCreated();
        return session;
    }

    public void End(DateTime endedAtUtc) {
        if (IsCompleted) {
            return;
        }

        EndedAtUtc = endedAtUtc;
        IsCompleted = true;
        SetModified();
    }

    public void UpdateNotes(string? notes) {
        var normalized = NormalizeNotes(notes);
        if (Notes == normalized) {
            return;
        }

        Notes = normalized;
        SetModified();
    }

    public void Extend(int additionalHours) {
        if (IsCompleted) {
            throw new InvalidOperationException("Cannot extend a completed fasting session.");
        }

        if (additionalHours <= 0) {
            throw new ArgumentOutOfRangeException(nameof(additionalHours), "Additional duration must be greater than zero.");
        }

        EnsureDuration(PlannedDurationHours + additionalHours);
        AddedDurationHours += additionalHours;
        SetModified();
    }

    public FastingSessionStatus GetStatus() {
        if (!EndedAtUtc.HasValue) {
            return FastingSessionStatus.Active;
        }

        if (IsIntermittentProtocol(Protocol)) {
            return FastingSessionStatus.Completed;
        }

        var targetReachedAtUtc = StartedAtUtc.AddHours(PlannedDurationHours);
        return EndedAtUtc.Value >= targetReachedAtUtc
            ? FastingSessionStatus.Completed
            : FastingSessionStatus.Interrupted;
    }

    public static int GetDefaultDuration(FastingProtocol protocol) => protocol switch {
        FastingProtocol.F16_8 => 16,
        FastingProtocol.F18_6 => 18,
        FastingProtocol.F20_4 => 20,
        FastingProtocol.F24_0 => 24,
        FastingProtocol.F36_0 => 36,
        FastingProtocol.F72_0 => 72,
        FastingProtocol.CustomIntermittent => 16,
        FastingProtocol.Custom => 16,
        _ => 16
    };

    private static bool IsIntermittentProtocol(FastingProtocol protocol) => protocol switch {
        FastingProtocol.F16_8 => true,
        FastingProtocol.F18_6 => true,
        FastingProtocol.F20_4 => true,
        FastingProtocol.CustomIntermittent => true,
        _ => false
    };

    private static string? NormalizeNotes(string? value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length > NotesMaxLength
            ? throw new ArgumentOutOfRangeException(nameof(value), $"Notes must be at most {NotesMaxLength} characters.")
            : trimmed;
    }

    private static void EnsureUserId(UserId userId) {
        if (userId == UserId.Empty) {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }
    }

    private static void EnsureDuration(int hours) {
        if (hours < MinDurationHours || hours > MaxDurationHours) {
            throw new ArgumentOutOfRangeException(nameof(hours), $"Duration must be between {MinDurationHours} and {MaxDurationHours} hours.");
        }
    }
}
